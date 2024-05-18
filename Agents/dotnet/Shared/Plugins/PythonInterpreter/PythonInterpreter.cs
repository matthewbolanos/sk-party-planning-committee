// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Humanizer;
using System.Text.Json.Serialization;

namespace PartyPlanning.Agents.Plugins.PythonInterpreter;

/// <summary>
/// A plugin for running Python code in an Azure Container Apps dynamic sessions code interpreter.
/// </summary>
public partial class PythonInterpreter
{
    private static readonly string s_assemblyVersion = typeof(Kernel).Assembly.GetName().Version!.ToString();

    private readonly Uri _poolManagementEndpoint;
    private readonly PythonInterpreterSettings _settings;
    private readonly Func<Task<string>>? _authTokenProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private Assembly _assembly = Assembly.GetExecutingAssembly();
    private readonly string _identifier;
    private readonly PydanticGenerator generator = new();

    /// <summary>
    /// Initializes a new instance of the PythonInterpreterTool class.
    /// </summary>
    /// <param name="settings">The settings for the Python tool plugin. </param>
    /// <param name="httpClientFactory">The HTTP client factory. </param>
    /// <param name="authTokenProvider"> Optional provider for auth token generation. </param>
    /// <param name="loggerFactory">The logger factory. </param>
    public PythonInterpreter(
        PythonInterpreterSettings settings,
        IHttpClientFactory httpClientFactory,
        Func<Task<string>>? authTokenProvider = null,
        ILoggerFactory? loggerFactory = null)
    {
        this._settings = settings;

        // Ensure the endpoint won't change by reference 
        this._poolManagementEndpoint = GetBaseEndpoint(settings.Endpoint);

        this._authTokenProvider = authTokenProvider;
        this._httpClientFactory = httpClientFactory;
        this._identifier = Guid.NewGuid().ToString();
        this._logger = loggerFactory?.CreateLogger(typeof(PythonInterpreter)) ?? NullLogger.Instance;

        InitializeAsync().Wait();
    }

    public async Task InitializeAsync()
    {
        using Stream resourceStream = _assembly.GetManifestResourceStream("PartyPlanning.Agents.Shared.scripts.setup_env.py")!;
        using StreamReader reader = new StreamReader(resourceStream);
        string setupEnvCode = reader.ReadToEnd();
        var setupEnvCodeResults = await BaseExecuteAsync(setupEnvCode);

        var filesToUpload = new[]
        {
            "PartyPlanning.Agents.Shared.scripts.main_runner.py",
            "PartyPlanning.Agents.Shared.scripts.modules.connection_helpers.py",
            "PartyPlanning.Agents.Shared.scripts.modules.function_helpers.py"
        };
        await Task.WhenAll(filesToUpload.Select(async resourcePath =>
        {
            using Stream resourceStream = _assembly.GetManifestResourceStream(resourcePath)!;
            using StreamReader reader = new StreamReader(resourceStream);
            string code = reader.ReadToEnd();

            // Get the file name (the last two portions of the resource path)
            var fileName = resourcePath.Split('.')[^2..].Aggregate((a, b) => a + "." + b);

            // Get the folder name (everything after python and before the file name)
            var folderName = resourcePath.Split('.')[3..^2].Aggregate((a, b) => a + "/" + b);

            // Turn the code into a binary stream
            byte[] fileBytes = Encoding.UTF8.GetBytes(code);

            var uploadResponse = await UploadBinaryAsync(fileBytes, folderName, fileName);
        }));
    }

    [KernelFunction("run")]
    [Description("Executes Python code in a container and returns the stdout, stderr, and result.")]
    public async Task<PythonInterpreterExecutionResult> ExecuteAsync(Kernel kernel, string code)
    {
        // Create plugin files and upload them to the Python container asynchronously
        var uploadTasks = new List<Task<PythonInterpreterFileUploadResponse>>();

        string pluginsCode = "from modules.function_helpers import poll_for_results, write_function_call\n\n";
        pluginsCode += generator.GeneratePluginCode(kernel, false);
        uploadTasks.Add(UploadBinaryAsync(Encoding.UTF8.GetBytes(pluginsCode), "scripts", $"functions.py"));

        // Wait for all the plugin files to be uploaded
        await Task.WhenAll(uploadTasks);

        // Save the main code to the python container
        // Replace "-" separators with "__" to avoid issues running the code
        foreach (var plugin in kernel.Plugins)
        {
            foreach (var function in plugin)
            {
                // Build function names with "-" separators
                var previousFunctionNameWithDash = plugin.Name + "-" + function.Name;
                var previousFunctionNameWithPeriod = plugin.Name + "." + function.Name;
                var previousFunctionNameWithUnderscore = plugin.Name + "_" + function.Name;
                var newFunctionName = plugin.Name + "__" + function.Name;

                // Replace previous function names with new function names
                code = code.Replace(previousFunctionNameWithDash, newFunctionName);
                code = code.Replace(previousFunctionNameWithPeriod, newFunctionName);
                code = code.Replace(previousFunctionNameWithUnderscore, newFunctionName);
            }
        }

        code = "import functions as functions\nfrom functions import *\n\n" + code;
        await UploadBinaryAsync(Encoding.UTF8.GetBytes(code), "scripts", "main.py");

        // Initialize the Python environment for a new execution
        using Stream startScriptResourceStream = _assembly.GetManifestResourceStream("PartyPlanning.Agents.Shared.scripts.start_script.py")!;
        using StreamReader startScriptReader = new(startScriptResourceStream);
        string startScriptCode = startScriptReader.ReadToEnd();
        var startScriptCodeResults = await BaseExecuteAsync(startScriptCode);

        int iterator = 0;
        var sendResponseScriptCode = "";

        // Loop until the final result is returned
        while (true)
        {
            // Create a connection to the Python container to get function calls or the final result
            using Stream connectionScriptResourceStream = _assembly.GetManifestResourceStream("PartyPlanning.Agents.Shared.scripts.connection.py")!;
            using StreamReader connectionScriptReader = new(connectionScriptResourceStream);
            string connectionScriptCode = connectionScriptReader.ReadToEnd();
            PythonInterpreterExecutionResult connectionScriptResults;
            try
            {
                connectionScriptResults = await BaseExecuteAsync(sendResponseScriptCode + connectionScriptCode);
            }
            catch
            {
                return new PythonInterpreterExecutionResult();
            }

            // if the connection script results in a function call, execute the function
            // we'll know if it's a function call if connectionScriptResults can be deserialized into a PythonExecutionFunctionCall
            var connectionResult = DeserializeConnectionJson(connectionScriptResults!.Result);


            switch (connectionResult)
            {
                case List<PythonInterpreterFunctionCallContent> functionCalls:
                    {
                        List<PythonInterpreterFunctionResultContent> functionResults = new();
                        foreach (var functionCall in functionCalls)
                        {
                            KernelArguments args = [];
                            if (functionCall!.Arguments != null && functionCall!.Arguments != "null")
                            {
                                var arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(functionCall!.Arguments);
                                if (arguments != null)
                                {
                                    foreach (var argument in arguments)
                                    {
                                        if (argument.Value is JsonElement jsonElement)
                                        {
                                            object deserializedValue;

                                            // Deserialize based on JsonElement type
                                            switch (jsonElement.ValueKind)
                                            {
                                                case JsonValueKind.Null:
                                                    deserializedValue = null;
                                                    break;
                                                case JsonValueKind.True:
                                                case JsonValueKind.False:
                                                    deserializedValue = jsonElement.GetBoolean();
                                                    break;
                                                case JsonValueKind.String:
                                                    deserializedValue = jsonElement.GetString();
                                                    break;
                                                case JsonValueKind.Number:
                                                    if (jsonElement.TryGetInt32(out int intVal))
                                                    {
                                                        deserializedValue = intVal;
                                                    }
                                                    else if (jsonElement.TryGetDouble(out double doubleVal))
                                                    {
                                                        deserializedValue = doubleVal;
                                                    }
                                                    else
                                                    {
                                                        deserializedValue = jsonElement.GetRawText();
                                                    }
                                                    break;
                                                case JsonValueKind.Object:
                                                case JsonValueKind.Array:
                                                    deserializedValue = jsonElement.GetRawText();
                                                    break;
                                                default:
                                                    deserializedValue = jsonElement.GetRawText();
                                                    break;
                                            }

                                            args.Add(argument.Key, deserializedValue);
                                        }
                                        else if (argument.Value is string stringValue)
                                        {
                                            args.Add(argument.Key, stringValue);
                                        }
                                        else
                                        {
                                            // Serialize the argument value
                                            args.Add(argument.Key, JsonSerializer.Serialize(argument.Value));
                                        }
                                    }
                                }
                            }

                            // Get the function
                            KernelFunction function = kernel.Plugins.GetFunction(functionCall!.PluginName, functionCall!.FunctionName);

                            // Invoke the function
                            FunctionResult results = await kernel.InvokeAsync(
                                function,
                                args
                            );

                            // Get the result type
                            KernelJsonSchema schema = function.Metadata.ReturnParameter.Schema;

                            string serializedResult = "null";

                            // if the resultType is System.Void, then the result is null
                            if (schema != null)
                            {
                                try {
                                    serializedResult = results.GetValue<RestApiOperationResponse>().Content.ToString()!;
                                } catch {
                                    serializedResult = JsonSerializer.Serialize(results.GetValue<object>());
                                }
                            }

                            // Add to list of functionResults
                            functionResults.Add(new PythonInterpreterFunctionResultContent
                            {
                                Id = functionCall!.Id,
                                PluginName = functionCall!.PluginName,
                                FunctionName = functionCall!.FunctionName,
                                Result = serializedResult
                            });
                        }

                        // Create a response 
                        string json = JsonSerializer.Serialize(functionResults);

                        // Tell the current Python execution that the response has been sent
                        sendResponseScriptCode = $$"""
                            from modules.function_helpers import write_function_result
                            data = {{json}}
                            write_function_result(data)


                            """;
                        break;
                    }
                default:
                    return connectionScriptResults!;
            }

            iterator++;
        }
    }

    
    private async Task<PythonInterpreterExecutionResult> BaseExecuteAsync(string code)
    {
        if (this._settings.SanitizeInput)
        {
            code = SanitizeCodeInput(code);
        }

        // _logger.LogTrace("Executing Python code: {Code}", code);

        using var httpClient = this._httpClientFactory.CreateClient();

        var requestBody = new
        {
            properties = new PythonInterpreterCodeExecutionProperties(this._settings, code)
        };

        await this.AddHeadersAsync(httpClient).ConfigureAwait(false);

        using var request = new HttpRequestMessage(HttpMethod.Post, this._poolManagementEndpoint + "python/execute")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };

        var response = await httpClient.SendAsync(request).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new HttpRequestException($"Failed to execute python code. Status: {response.StatusCode}. Details: {errorBody}.");
        }

        return JsonSerializer.Deserialize<PythonInterpreterExecutionResult>(await response.Content.ReadAsStringAsync().ConfigureAwait(false))!;

    }

    private async Task AddHeadersAsync(HttpClient httpClient)
    {
        if (this._authTokenProvider is not null)
        {
            string authToken = await this._authTokenProvider();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
        }
    }

    /// <summary>
    /// Upload a file to the session pool.
    /// </summary>
    /// <param name="remoteFilePath">The path to the file in the session.</param>
    /// <param name="localFilePath">The path to the file on the local machine.</param>
    /// <returns>The metadata of the uploaded file.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    [KernelFunction("upload_file"), Description("Uploads a file to `/mnt/data` in the session")]
    public async Task<PythonInterpreterFileUploadResponseFile> UploadFileAsync(
        [Description("The path to the file in the session, relative to `/mnt/data`.")] string remoteFilePath,
        [Description("The path to the file on the local machine.")] string? localFilePath)
    {
        // this._logger.LogInformation("Uploading file: {LocalFilePath} to {RemoteFilePath}", localFilePath, remoteFilePath);

        using var httpClient = this._httpClientFactory.CreateClient();

        await this.AddHeadersAsync(httpClient).ConfigureAwait(false);

        using var fileContent = new ByteArrayContent(File.ReadAllBytes(localFilePath!));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{this._poolManagementEndpoint}python/uploadFile?identifier={this._settings.SessionId}")
        {
            Content = new MultipartFormDataContent
            {
                { fileContent, "file", remoteFilePath },
            }
        };

        var response = await httpClient.SendAsync(request).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new HttpRequestException($"Failed to upload file. Status code: {response.StatusCode}. Details: {errorBody}.");
        }

        var JsonElementResult = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));

        return JsonSerializer.Deserialize<PythonInterpreterFileUploadResponseFile>(JsonElementResult.GetProperty("$values")[0].GetRawText())!;
    }

    private async Task<PythonInterpreterFileUploadResponse> UploadBinaryAsync(
    byte[] fileBytes,
    string? targetFolder,
    string newFileName)
    {
        // Create the multipart/form-data content
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        // 'file' is the name of the form field that the server expects
        content.Add(fileContent, "file", newFileName);

        using var httpClient = this._httpClientFactory.CreateClient();

        await this.AddHeadersAsync(httpClient).ConfigureAwait(false);

        // Create the request
        var request = new HttpRequestMessage(HttpMethod.Post, $"{this._poolManagementEndpoint}python/uploadFile?identifier={this._settings.SessionId}")
        {
            Content = content
        };

        // Send the request
        var response = await httpClient.SendAsync(request);

        // Read the response
        var responseContent = response.Content.ReadAsStringAsync().Result;

        // Deserialize the response
        var executionResponse = JsonSerializer.Deserialize<PythonInterpreterFileUploadResponse>(responseContent);

        // Move the file to the target folder if specified using ExecuteAsync
        foreach (var file in executionResponse!.Values)
        {
            if (targetFolder != null)
            {
                var moveFileCode = $"""
                    import shutil
                    shutil.move('/mnt/data/{file.Filename}', '/mnt/data/{targetFolder}/{newFileName}')
                    """;
                await BaseExecuteAsync(moveFileCode);

                file.FullPath = $"/mnt/data/{targetFolder}/{newFileName}";
            }
            else
            {
                file.FullPath = $"/mnt/data/{newFileName}";
            }
        }

        return executionResponse!;
    }

    /// <summary>
    /// Downloads a file from the current Session ID.
    /// </summary>
    /// <param name="remoteFilePath"> The path to download the file from, relative to `/mnt/data`. </param>
    /// <param name="localFilePath"> The path to save the downloaded file to. If not provided won't save it in the disk.</param>
    /// <returns> The data of the downloaded file as byte array. </returns>
    [Description("Downloads a file from the current Session ID.")]
    public async Task<byte[]> DownloadFileAsync(
        [Description("The path to download the file from, relative to `/mnt/data`.")] string remoteFilePath,
        [Description("The path to save the downloaded file to. If not provided won't save it in the disk.")] string? localFilePath = null)
    {
        // this._logger.LogTrace("Downloading file: {RemoteFilePath} to {LocalFilePath}", remoteFilePath, localFilePath);

        using var httpClient = this._httpClientFactory.CreateClient();
        await this.AddHeadersAsync(httpClient).ConfigureAwait(false);

        var response = await httpClient.GetAsync(new Uri($"{this._poolManagementEndpoint}python/downloadFile?identifier={this._settings.SessionId}&filename={remoteFilePath}")).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new HttpRequestException($"Failed to download file. Status code: {response.StatusCode}. Details: {errorBody}.");
        }

        var fileContent = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(localFilePath))
        {
            try
            {
                File.WriteAllBytes(localFilePath, fileContent);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to write file to disk.", ex);
            }
        }

        return fileContent;
    }

    /// <summary>
    /// Lists all files in the provided session id pool.
    /// </summary>
    /// <returns> The list of files in the session. </returns>
    [KernelFunction, Description("Lists all files in the provided session id pool.")]
    public async Task<IReadOnlyList<PythonInterpreterFileUploadResponseFile>> ListFilesAsync()
    {
        // this._logger.LogTrace("Listing files for Session ID: {SessionId}", this._settings.SessionId);

        using var httpClient = this._httpClientFactory.CreateClient();
        await this.AddHeadersAsync(httpClient).ConfigureAwait(false);

        var response = await httpClient.GetAsync(new Uri($"{this._poolManagementEndpoint}python/files?identifier={this._settings.SessionId}")).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to list files. Status code: {response.StatusCode}");
        }

        var jsonElementResult = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));

        var files = jsonElementResult.GetProperty("$values");

        var result = new PythonInterpreterFileUploadResponseFile[files.GetArrayLength()];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = JsonSerializer.Deserialize<PythonInterpreterFileUploadResponseFile>(files[i].GetRawText())!;
        }

        return result;
    }

    private static Uri GetBaseEndpoint(Uri endpoint)
    {
        if (endpoint.PathAndQuery.Contains("/python/execute"))
        {
            endpoint = new Uri(endpoint.ToString().Replace("/python/execute", ""));
        }

        if (!endpoint.PathAndQuery.EndsWith("/", StringComparison.InvariantCulture))
        {
            endpoint = new Uri(endpoint + "/");
        }

        return endpoint;
    }

    /// <summary>
    /// Sanitize input to the python REPL.
    /// Remove whitespace, backtick and "python" (if llm mistakes python console as terminal)
    /// </summary>
    /// <param name="code">The code to sanitize</param>
    /// <returns>The sanitized code</returns>
    private static string SanitizeCodeInput(string code)
    {
        // Remove leading whitespace and backticks and python (if llm mistakes python console as terminal)
        code = RemoveLeadingWhitespaceBackticksPython().Replace(code, "");

        // Remove trailing whitespace and backticks
        code = RemoveTrailingWhitespaceBackticks().Replace(code, "");

        return code;
    }

    private static object? DeserializeConnectionJson(string json)
    {
        if (json == null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<PythonInterpreterFunctionCallContent>>(json)!;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public string GenerateMockPluginCodeForKernel(Kernel kernel)
    {
        return generator.GeneratePluginCode(kernel, true);
    }

    

#if NET
    [GeneratedRegex(@"^(\s|`)*(?i:python)?\s*", RegexOptions.ExplicitCapture)]
    private static partial Regex RemoveLeadingWhitespaceBackticksPython();

    [GeneratedRegex(@"(\s|`)*$", RegexOptions.ExplicitCapture)]
    private static partial Regex RemoveTrailingWhitespaceBackticks();
#else
    private static Regex RemoveLeadingWhitespaceBackticksPython() => s_removeLeadingWhitespaceBackticksPython;
    private static readonly Regex s_removeLeadingWhitespaceBackticksPython = new(@"^(\s|`)*(?i:python)?\s*", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    private static Regex RemoveTrailingWhitespaceBackticks() => s_removeTrailingWhitespaceBackticks;
    private static readonly Regex s_removeTrailingWhitespaceBackticks = new(@"(\s|`)*$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
#endif
}
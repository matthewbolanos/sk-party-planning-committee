// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using PartyPlanning.Agents.Shared.Plugins.PythonPlanner.CodeGen;
using PartyPlanning.Agents.Shared.Plugins.PythonPlanner.CodeGen.Models;

namespace PartyPlanning.Agents.Shared.Plugins.PythonPlanner;

/// <summary>
/// A plugin for running Python code in an Azure Container Apps dynamic sessions code interpreter.
/// </summary>
public partial class PythonPlanner
{
    private static readonly string s_assemblyVersion = typeof(Kernel).Assembly.GetName().Version!.ToString();

    private readonly Uri _poolManagementEndpoint;
    private readonly Func<Task<string>>? _authTokenProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PythonPlannerExecutionSettings _settings;
    private readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    private readonly PythonPluginGenerator generator = new();
    private readonly ILogger _logger;
    private readonly string _identifier;
    private readonly HashSet<KernelFunction> _kernelFunctions = [];
    private bool _isInitialized = false;

    /// <summary>
    /// Initializes a new instance of the PythonPlannerTool class.
    /// </summary>
    /// <param name="settings">The settings for the Python tool plugin. </param>
    /// <param name="httpClientFactory">The HTTP client factory. </param>
    /// <param name="authTokenProvider"> Optional provider for auth token generation. </param>
    /// <param name="loggerFactory">The logger factory. </param>
    public PythonPlanner(
        PythonPlannerExecutionSettings settings,
        IHttpClientFactory httpClientFactory,
        Func<Task<string>>? authTokenProvider = null,
        ILoggerFactory? loggerFactory = null)
    {
        // Setup settings
        this._settings = settings;
        this._poolManagementEndpoint = GetBaseEndpoint(settings.Endpoint);

        this._authTokenProvider = authTokenProvider;
        this._httpClientFactory = httpClientFactory;
        this._identifier = Guid.NewGuid().ToString();
        this._logger = loggerFactory?.CreateLogger(typeof(PythonPlanner)) ?? NullLogger.Instance;
    }

    /// <summary>
    /// Initializes the Python environment for a new execution.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InitializeAsync(Kernel kernel)
    {
        // Run setup script which does the following:
        // 1. Setup caching for Librosa cache
        // 2. Setup __init__.py files in the scripts folder and its subfolders
        // 3. Setup environment variables

        var initCode = await generator.GeneratePythonSetupCodeAsync(kernel);
        await BaseExecuteAsync(initCode);
    }

    /// <summary>
    /// Executes Python code in a container and returns the stdout, stderr, and result.
    /// </summary>
    /// <param name="kernel">The kernel.</param>
    /// <param name="code">The Python code to execute.</param>
    /// <returns>The Python execution result.</returns>
    [KernelFunction("run")]
    [Description("Executes Python code in a jupyter notebook cell and returns the stdout, stderr, and result.")]
    public async Task<PythonPlannerResult> ExecuteAsync(Kernel kernel, string code)
    {
        if (!_isInitialized)
        {
            await InitializeAsync(kernel);
            _isInitialized = true;
        }

        // This is the main function that is provided to the LLM to execute Python code in the Python container
        // The code is executed in the following steps:
        // 1. The code is sanitized to remove any leading or trailing whitespace, backticks, or the word "python"
        // 2. Determine which functions are "new" and need to be uploaded to the Python container
        // 3. Generate Python code to add the new functions to the Python container
        // 4. Standardization of function names is done (TODO: align separators across Semantic Kernel to make this unnecessary)
        // 5. Run the start_script.py to add the new functions to the Python container and execute the main code
        // 6. Perform "function calling" loop until the final result is returned

        // 1. Sanitize the input
        ////////////////////////////////////////////////
        if (_settings.SanitizeInput)
        {
            code = SanitizeCodeInput(code);
        }

        // 2. Check for "new" functions
        ////////////////////////////////////////////////
        HashSet<KernelFunction> kernelFunctions = kernel.Plugins.SelectMany(p => p).ToHashSet();
        var newFunctions = kernelFunctions.Except(_kernelFunctions).ToList();
        _kernelFunctions.UnionWith(kernelFunctions);
        
        // 3. Generate Python code for new functions
        ////////////////////////////////////////////////
        var newFunctionsCode = "";
        if (newFunctions.Count != 0)
        {
            newFunctionsCode = await generator.GeneratePythonFunctionsAsync(kernel, new (){
                FunctionFilters = new FunctionFilters
                {
                    IncludedFunctions = newFunctions.Select(f => new FunctionName(f.PluginName!, f.Name)).ToList()
                }
            });

            string newFunctionsScript = await generator.GeneratePythonRunCodeAsync(kernel, newFunctionsCode);
            var newFunctionsCodeResults = await BaseExecuteAsync(newFunctionsScript);
        }

        // 4. Standardize function names
        ////////////////////////////////////////////////
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

        // Remove any lines that start with "import functions" or "from functions import"
        code = RemoveImportAndFromFunctionsLines(code);

        // 5. Upload the main code and any new plugins
        ////////////////////////////////////////////////
        string startScriptCode = await generator.GeneratePythonRunCodeAsync(kernel, code);
        var startScriptCodeResults = await BaseExecuteAsync(startScriptCode);


        // 6. Perform "function calling" loop 
        ////////////////////////////////////////////////

        List<PythonPlannerFunctionResultContent> functionResults = [];
        while (true)
        {
            // 6.a. Create a relay to the Python container
            ////////////////////////////////////////////////
            var relayCode = await generator.GeneratePythonRelayCodeAsync(kernel, functionResults, startScriptCodeResults.Result);
            var relayCodeResults = await BaseExecuteAsync(relayCode);

            // 6.b. Deserialize the relay code results
            ////////////////////////////////////////////////

            try {
                var relayCodeResultsDeserialized = JsonSerializer.Deserialize<List<PythonPlannerFunctionCallContent>>(relayCodeResults.Result)!;
                List<Task> functionTasks = [];

                foreach (var functionCall in relayCodeResultsDeserialized)
                {
                    functionTasks.Add(Task.Run(async () =>
                    {
                        var function = kernel.Plugins.GetFunction(functionCall.PluginName, functionCall.FunctionName);
                        var args = JsonSerializer.Deserialize<Dictionary<string, object?>?>(functionCall.Arguments);
                        KernelArguments kernelArgs = [];

                        // Add parameters to arguments
                        if (args is not null)
                        {
                            foreach (var parameter in args)
                            {
                                kernelArgs[parameter.Key] = parameter.Value?.ToString();
                            }
                        }

                        var results = await kernel.InvokeAsync(function, kernelArgs);
                        var schema = function.Metadata.ReturnParameter.Schema;
                        string serializedResult = "null";
                        if (schema != null)
                        {
                            try {
                                serializedResult = results.GetValue<RestApiOperationResponse>()!.Content.ToString()!;
                            } catch {
                                serializedResult = JsonSerializer.Serialize(results.GetValue<object>());
                            }
                        }
                        functionResults.Add(new PythonPlannerFunctionResultContent
                        {
                            Id = functionCall.Id,
                            PluginName = functionCall.PluginName,
                            FunctionName = functionCall.FunctionName,
                            Result = serializedResult
                        });
                    }));
                }

                await Task.WhenAll(functionTasks);
            } catch {
                // 7. Return the final result
                ////////////////////////////////////////////////
                return JsonSerializer.Deserialize<PythonPlannerResult>(relayCodeResults.Result)!;
            }
        }
    }
    private async Task<PythonPlannerResult> BaseExecuteAsync(string code)
    {
        // log the code
        this._logger.LogTrace("Executing code: {Code}", code);

        using var httpClient = this._httpClientFactory.CreateClient();

        var requestBody = new
        {
            properties = new CodeInterpreterRequestSettings(this._settings, code)
        };

        await AddHeadersAsync(httpClient).ConfigureAwait(false);

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

        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        // log the response
        this._logger.LogTrace("Response: {Response}", responseContent);

        return JsonSerializer.Deserialize<PythonPlannerResult>(responseContent)!;
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
    public async Task<PythonPlannerFileUploadResponseFile> UploadFileAsync(
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

        return JsonSerializer.Deserialize<PythonPlannerFileUploadResponseFile>(JsonElementResult.GetProperty("$values")[0].GetRawText())!;
    }

    private async Task<PythonPlannerFileUploadResponse> UploadBinaryAsync(
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
        var executionResponse = JsonSerializer.Deserialize<PythonPlannerFileUploadResponse>(responseContent);

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
    public async Task<IReadOnlyList<PythonPlannerFileUploadResponseFile>> ListFilesAsync()
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

        var result = new PythonPlannerFileUploadResponseFile[files.GetArrayLength()];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = JsonSerializer.Deserialize<PythonPlannerFileUploadResponseFile>(files[i].GetRawText())!;
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

    public async Task<string> GenerateMockPluginCodeForKernelAsync(Kernel kernel)
    {
        return await generator.GeneratePythonPluginManualAsync(kernel);
    }

    static string RemoveImportAndFromFunctionsLines(string code)
    {
        // Use regex to match lines starting with "import functions" or "from functions"
        string pattern = @"^(import functions|from functions).*?(\r?\n|$)";
        return Regex.Replace(code, pattern, string.Empty, RegexOptions.Multiline);
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
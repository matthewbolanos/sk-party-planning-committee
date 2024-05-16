// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.SemanticKernel.Plugins.Core.CodeInterpreter;

/// <summary>
/// A plugin for running Python code in an Azure Container Apps dynamic sessions code interpreter.
/// </summary>
public partial class SessionsPythonPlugin
{
    private static readonly string s_assemblyVersion = typeof(Kernel).Assembly.GetName().Version!.ToString();

    private readonly Uri _poolManagementEndpoint;
    private readonly SessionsPythonSettings _settings;
    private readonly Func<Task<string>>? _authTokenProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the SessionsPythonTool class.
    /// </summary>
    /// <param name="settings">The settings for the Python tool plugin. </param>
    /// <param name="httpClientFactory">The HTTP client factory. </param>
    /// <param name="authTokenProvider"> Optional provider for auth token generation. </param>
    /// <param name="loggerFactory">The logger factory. </param>
    public SessionsPythonPlugin(
        SessionsPythonSettings settings,
        IHttpClientFactory httpClientFactory,
        Func<Task<string>>? authTokenProvider = null,
        ILoggerFactory? loggerFactory = null)
    {
        this._settings = settings;

        // Ensure the endpoint won't change by reference 
        this._poolManagementEndpoint = GetBaseEndpoint(settings.Endpoint);

        this._authTokenProvider = authTokenProvider;
        this._httpClientFactory = httpClientFactory;
        this._logger = loggerFactory?.CreateLogger(typeof(SessionsPythonPlugin)) ?? NullLogger.Instance;
    }

    /// <summary>
    /// Executes the provided Python code.
    /// Start and end the code snippet with double quotes to define it as a string.
    /// Insert \n within the string wherever a new line should appear.
    /// Add spaces directly after \n sequences to replicate indentation.
    /// Use \"" to include double quotes within the code without ending the string.
    /// Keep everything in a single line; the \n sequences will represent line breaks
    /// when the string is processed or displayed.
    /// </summary>
    /// <param name="code"> The valid Python code to execute. </param>
    /// <returns> The result of the Python code execution. </returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    [KernelFunction, Description("""
        Executes the provided Python code.
        Start and end the code snippet with double quotes to define it as a string.
        Insert \n within the string wherever a new line should appear.
        Add spaces directly after \n sequences to replicate indentation.
        Use \" to include double quotes within the code without ending the string.
        Keep everything in a single line; the \n sequences will represent line breaks
        when the string is processed or displayed.
        """)]
    public async Task<string> ExecuteCodeAsync([Description("The valid Python code to execute.")] string code)
    {
        if (this._settings.SanitizeInput)
        {
            code = SanitizeCodeInput(code);
        }

        this._logger.LogTrace("Executing Python code: {Code}", code);

        using var httpClient = this._httpClientFactory.CreateClient();

        var requestBody = new
        {
            properties = new SessionsPythonCodeExecutionProperties(this._settings, code)
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

        var jsonElementResult = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));

        return $"""
            Result:
            {jsonElementResult.GetProperty("result").GetRawText()}
            Stdout:
            {jsonElementResult.GetProperty("stdout").GetRawText()}
            Stderr:
            {jsonElementResult.GetProperty("stderr").GetRawText()}
            """;
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
    [KernelFunction, Description("Uploads a file for the current session id pool.")]
    public async Task<SessionsRemoteFileMetadata> UploadFileAsync(
        [Description("The path to the file in the session, relative to `/mnt/data`.")] string remoteFilePath,
        [Description("The path to the file on the local machine.")] string? localFilePath)
    {
        this._logger.LogInformation("Uploading file: {LocalFilePath} to {RemoteFilePath}", localFilePath, remoteFilePath);

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

        return JsonSerializer.Deserialize<SessionsRemoteFileMetadata>(JsonElementResult.GetProperty("$values")[0].GetRawText())!;
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
        this._logger.LogTrace("Downloading file: {RemoteFilePath} to {LocalFilePath}", remoteFilePath, localFilePath);

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
    public async Task<IReadOnlyList<SessionsRemoteFileMetadata>> ListFilesAsync()
    {
        this._logger.LogTrace("Listing files for Session ID: {SessionId}", this._settings.SessionId);

        using var httpClient = this._httpClientFactory.CreateClient();
        await this.AddHeadersAsync(httpClient).ConfigureAwait(false);

        var response = await httpClient.GetAsync(new Uri($"{this._poolManagementEndpoint}python/files?identifier={this._settings.SessionId}")).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to list files. Status code: {response.StatusCode}");
        }

        var jsonElementResult = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));

        var files = jsonElementResult.GetProperty("$values");

        var result = new SessionsRemoteFileMetadata[files.GetArrayLength()];

        for (var i = 0; i < result.Length; i++)
        {
            result[i] = JsonSerializer.Deserialize<SessionsRemoteFileMetadata>(files[i].GetRawText())!;
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
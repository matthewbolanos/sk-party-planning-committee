// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace PartyPlanning.Agents.Plugins.PythonInterpreter;

/// <summary>
/// Settings for a Python Sessions Plugin.
/// </summary>
public class PythonInterpreterSettings
{
    /// <summary>
    /// Determines if the input should be sanitized.
    /// </summary>
    [JsonIgnore]
    public bool SanitizeInput { get; set; }

    /// <summary>
    /// The target endpoint.
    /// </summary>
    [JsonIgnore]
    public Uri Endpoint { get; set; }

    /// <summary>
    /// The session identifier.
    /// </summary>
    [JsonPropertyName("identifier")]
    public string SessionId { get; set; }

    /// <summary>
    /// Code input type.
    /// </summary>
    [JsonPropertyName("codeInputType")]
    public CodeInputTypeSetting CodeInputType { get; set; } = CodeInputTypeSetting.Inline;

    /// <summary>
    /// Code execution type.
    /// </summary>
    [JsonPropertyName("executionType")]
    public CodeExecutionTypeSetting CodeExecutionType { get; set; } = CodeExecutionTypeSetting.Synchronous;

    /// <summary>
    /// Timeout in seconds for the code execution.
    /// </summary>
    [JsonPropertyName("timeoutInSeconds")]
    public int TimeoutInSeconds { get; set; } = 10;

    /// <summary>
    /// Initializes a new instance of the <see cref="PythonInterpreterSettings"/> class.
    /// </summary>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="endpoint">Azure Container Apps Endpoint.</param>
    [JsonConstructor]
    public PythonInterpreterSettings(string sessionId, Uri endpoint)
    {
        this.SessionId = sessionId;
        this.Endpoint = endpoint;
    }

    /// <summary>
    /// Code input type.
    /// </summary>
    [Description("Code input type.")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CodeInputTypeSetting
    {
        /// <summary>
        /// Code is provided as a inline string.
        /// </summary>
        [Description("Code is provided as a inline string.")]
        [JsonPropertyName("inline")]
        Inline
    }

    /// <summary>
    /// Code input type.
    /// </summary>
    [Description("Code input type.")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CodeExecutionTypeSetting
    {
        /// <summary>
        /// Code is provided as a inline string.
        /// </summary>
        [Description("Code is provided as a inline string.")]
        [JsonPropertyName("synchronous")]
        Synchronous
    }
}
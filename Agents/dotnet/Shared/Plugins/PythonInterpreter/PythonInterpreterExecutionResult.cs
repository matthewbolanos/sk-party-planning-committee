using System.Text.Json.Serialization;


namespace PartyPlanning.Agents.Plugins.PythonInterpreter;

[JsonConverter(typeof(PythonInterpreterExecutionResultResultConverter))]
public class PythonInterpreterExecutionResult
{
    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("stdout")]
    public string Stdout { get; set; }

    [JsonPropertyName("stderr")]
    public string Stderr { get; set; }

    [JsonPropertyName("result")]
    public string Result { get; set; }

    [JsonPropertyName("executionTimeInMilliseconds")]
    public int ExecutionTimeInMilliseconds { get; set; }
}
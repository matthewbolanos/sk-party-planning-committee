using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;

namespace PartyPlanning.Agents.Plugins.PythonInterpreter;

#pragma warning disable SKEXP0001
public class PythonInterpreterFunctionCallContent
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("plugin_name")]
    public string PluginName { get; set; }

    [JsonPropertyName("function_name")]
    public string FunctionName { get; set; }

    [JsonPropertyName("args")]
    public string Arguments { get; set; }

    [JsonPropertyName("waited_time")]
    public float WaitedTime { get; set; }
}

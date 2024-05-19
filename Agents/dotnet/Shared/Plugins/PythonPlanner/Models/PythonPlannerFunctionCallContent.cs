using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;

namespace PartyPlanning.Agents.Shared.Plugins.PythonPlanner;

#pragma warning disable SKEXP0001
public class PythonPlannerFunctionCallContent
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("plugin")]
    public string PluginName { get; set; }

    [JsonPropertyName("function")]
    public string FunctionName { get; set; }

    [JsonPropertyName("args")]
    public string Arguments { get; set; }
}

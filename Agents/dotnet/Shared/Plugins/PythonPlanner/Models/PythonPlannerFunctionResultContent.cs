using System.Text.Json.Serialization;

namespace PartyPlanning.Agents.Shared.Plugins.PythonPlanner;
public class PythonPlannerFunctionResultContent
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("plugin_name")]
    public string PluginName { get; set; }

    [JsonPropertyName("function_name")]
    public string FunctionName { get; set; }

    [JsonPropertyName("result")]
    public string Result { get; set; }
}
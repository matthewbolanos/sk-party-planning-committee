using System.Text.Json.Serialization;

namespace PartyPlanning.Agents.Plugins.PythonInterpreter;
public class PythonInterpreterFunctionResultContent
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
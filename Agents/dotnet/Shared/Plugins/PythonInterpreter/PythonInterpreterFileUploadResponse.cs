
using System.Text.Json.Serialization;

namespace PartyPlanning.Agents.Plugins.PythonInterpreter;

public class PythonInterpreterFileUploadResponse
{
    [JsonPropertyName("$id")]
    public string Id { get; set; }

    [JsonPropertyName("$values")]
    public List<PythonInterpreterFileUploadResponseFile> Values { get; set; }
}
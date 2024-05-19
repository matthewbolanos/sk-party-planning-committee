
using System.Text.Json.Serialization;

namespace PartyPlanning.Agents.Shared.Plugins.PythonPlanner;

public class PythonPlannerFileUploadResponse
{
    [JsonPropertyName("$id")]
    public string Id { get; set; }

    [JsonPropertyName("$values")]
    public List<PythonPlannerFileUploadResponseFile> Values { get; set; }
}
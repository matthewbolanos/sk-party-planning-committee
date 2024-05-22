using System.Text.Json.Serialization;

namespace PartyPlanning.Agents.Shared.Plugins.PythonPlanner;

public class PythonPlannerFileUploadResponseFile
{
    [JsonPropertyName("$id")]
    public string Id { get; set; }

    [JsonPropertyName("filename")]
    public string Filename { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("last_modified_time")]
    public DateTime LastModifiedTime { get; set; }
}
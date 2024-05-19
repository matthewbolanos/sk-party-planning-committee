using System.Text.Json.Serialization;

namespace PartyPlanning.Agents.Shared.Plugins.PythonPlanner;

public class PythonPlannerFileUploadResponseFile
{
    [JsonPropertyName("$id")]
    public string Id { get; set; }

    [JsonPropertyName("filename")]
    public string Filename { get; set; }

    [JsonPropertyName("bytes")]
    public int Bytes { get; set; }

    [JsonPropertyName("lastModifiedTime")]
    public DateTime LastModifiedTime { get; set; }

    [JsonPropertyName("fullPath")]
    public string? FullPath { get; set; }
}
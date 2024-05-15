using System;
using System.Text.Json.Serialization;

namespace PartyPlanning.Agents.Shared.Models
{
    /// <summary>
    /// Model representing a run within a thread.
    /// </summary>
    public class AssistantThreadRun
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? ThreadId { get; set; }

        [JsonPropertyName("assistant_id")]
        public string? AssistantId { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = true;

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

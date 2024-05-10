using System;

namespace Shared.Models
{
    /// <summary>
    /// Model representing a run within a thread.
    /// </summary>
    public class Run
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? ThreadId { get; set; }
        public string? AssistantId { get; set; }
        public string? Model { get; set; }
        public bool Stream { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

using System.Text.Json.Serialization;

namespace Shared.Models
{
    /// <summary>
    /// Represents a thread input model.
    /// </summary>
    public class ThreadInputModel
    {
        [JsonPropertyName("messages")]
        public List<AssistantMessageContentInputModel> Messages { get; set; } = [];
    }

}
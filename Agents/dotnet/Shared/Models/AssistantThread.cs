using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace Shared.Models
{
    /// <summary>
    /// Represents a thread resource.
    /// </summary>
    public class AssistantThread : AssistantThreadBase
    {
        public List<AssistantMessageContent> Messages { get; set; } = [];
    }
}
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace Shared.Models
{
    /// <summary>
    /// Represents a thread resource.
    /// </summary>
    public class AssistantThread
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public string Object { get; set; } = "thread";
        public List<ThreadMessageContent> Messages { get; set; } = [];
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
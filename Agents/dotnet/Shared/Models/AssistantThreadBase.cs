using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace Shared.Models
{
    /// <summary>
    /// Represents a base thread resource
    /// </summary>
    public class AssistantThreadBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public string Object { get; set; } = "thread";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
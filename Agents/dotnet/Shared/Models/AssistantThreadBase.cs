using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace PartyPlanning.Agents.Shared.Models
{
    /// <summary>
    /// Represents a base thread resource
    /// </summary>
    public class AssistantThreadBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("object")]
        [JsonPropertyName("object")]
        public string Object { get; set; } = "thread";

        [BsonElement("created_at")]
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
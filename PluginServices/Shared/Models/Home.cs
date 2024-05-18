using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PartyPlanning.PluginServices.Shared.Models
{
    /// <summary>
    /// Represents a home with multiple rooms.
    /// </summary>
    public class Home(string name)
    {
        /// <summary>
        /// The unique identifier of the HomeService.
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        /// <summary>
        /// The name of the HomeService.
        /// </summary>
        public string Name { get; set; } = name;

        /// <summary>
        /// The rooms in the HomeService.
        /// </summary>
        public List<Room> Rooms { get; set; } = [];
    }
}

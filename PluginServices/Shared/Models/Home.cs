using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Shared.Models
{
    /// <summary>
    /// Represents a home with multiple rooms.
    /// </summary>
    public class Home(string name)
    {
        /// <summary>
        /// The unique identifier of the home.
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        /// <summary>
        /// The name of the home.
        /// </summary>
        public string Name { get; set; } = name;

        /// <summary>
        /// The rooms in the home.
        /// </summary>
        public List<Room> Rooms { get; set; } = [];
    }
}

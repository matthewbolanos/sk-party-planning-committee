using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PartyPlanning.PluginServices.Shared.Models
{
    ///<summary>
    /// Represents an abstract smart device that can be inherited by other devices.
    ///</summary>
    public class SmartDevice(string name, string type)
    {
        /// <summary>
        /// The unique identifier of the device.
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        
        /// <summary>
        /// The name of the smart device.
        /// </summary>
        public string Name { get; set; } = name;

        /// <summary>
        /// The type of the smart device.
        /// </summary>
        public string Type { get; set; } = type;
    }
}

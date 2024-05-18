using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PartyPlanning.PluginServices.Shared.Models;

/// <summary>
/// Represents a room with multiple smart devices.
/// </summary>
public class Room(string name)
{
    /// <summary>
    /// The unique identifier of the room.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>
    /// The name of the room.
    /// </summary>
    public string Name { get; set; } = name ?? throw new ArgumentNullException(nameof(name));

    /// <summary>
    /// A list of ObjectIds representing smart devices in the room.
    /// </summary>
    [BsonElement("DeviceIds")]
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> DeviceIds { get; set; } = new();
}
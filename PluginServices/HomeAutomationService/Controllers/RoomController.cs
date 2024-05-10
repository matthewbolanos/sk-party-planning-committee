using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Shared.Models;

namespace HomeAutomation.Services
{
    /// <summary>
    /// Service class providing room-related operations.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="RoomService"/> class.
    /// </remarks>
    /// <param name="database">The MongoDB database instance.</param>
    [ApiController]
    [Route("api/rooms")]
    public class RoomService(IMongoDatabase database) : ControllerBase
    {
        private readonly IMongoCollection<Home> _homes = database.GetCollection<Home>("Homes");
        private readonly IMongoCollection<SmartDevice> _smartDevices = database.GetCollection<SmartDevice>("SmartDevices");

        /// <summary>
        /// Get the rooms in the home.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> containing the list of rooms.</returns>
        [HttpGet("/api/home/rooms", Name="get_rooms")]
        public IActionResult GetRooms()
        {
            var home = _homes.Find(_ => true).FirstOrDefault();
            if (home == null) return NotFound();

            // Adjust the returned rooms list to include the correct ID field
            var rooms = home.Rooms.Select(room => new
            {
                room.Id,
                room.Name,
                room.DeviceIds
            });

            return Ok(rooms);
        }

        /// <summary>
        /// Retrieves a room by its unique identifier asynchronously.
        /// </summary>
        /// <param name="roomId">The unique identifier of the room.</param>
        /// <returns>An <see cref="IActionResult"/> containing the room or an appropriate error response.</returns>
        [HttpGet("/api/home/rooms/{roomId}", Name="get_room")]
        public async Task<IActionResult> GetRoomByIdAsync(string roomId)
        {
            var home = await _homes.Find(_ => true).FirstOrDefaultAsync();
            if (home == null) return NotFound("Home not found.");

            var room = home.Rooms.Find(r => r.Id == roomId);
            if (room == null) return NotFound($"Room with ID {roomId} not found.");

            // Directly use string DeviceIds instead of converting to ObjectIds
            var filter = Builders<SmartDevice>.Filter.In("_id", room.DeviceIds);
            var devices = await _smartDevices.Find(filter).ToListAsync();

            var roomWithDevices = new
            {
                room.Id,
                room.Name,
                Devices = devices
            };

            return Ok(roomWithDevices);
        }

    }
}

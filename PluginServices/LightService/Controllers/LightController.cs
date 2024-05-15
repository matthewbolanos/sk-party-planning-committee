using PartyPlanning.Agents.PartyPlanning.Agents.Shared.Serializers;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using LightService.Models;
using MongoDB.Bson;

namespace LightService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LightController(IMongoDatabase database) : ControllerBase
    {
        private readonly IMongoCollection<BsonDocument> _smartDevices = database.GetCollection<BsonDocument>("SmartDevices");
        private readonly IMongoCollection<Light> _lights = database.GetCollection<Light>("Lights");
        private const int LatencyBuffer = 300; // milliseconds

        /// <summary>
        /// Retrieves all lights in the system.
        /// </summary>
        /// <returns>Returns the current state of the light and its 6 character long ID for other API requests</returns>
        [HttpGet(Name="get_all_lights")]
        public IActionResult GetLights()
        {
            // Fetch information about all devices of type "Light"
            var smartDevices = _smartDevices
                .Find(new BsonDocument { { "Type", "Light" } })
                .ToList();

            var deviceIds = smartDevices.Select(doc => doc["_id"].AsString).ToList();

            // Retrieve additional light-specific data
            var lights = _lights
                .Find(Builders<Light>.Filter.In(d => d.Id, deviceIds))
                .ToList();

            // Combine data from SmartDevices and Lights collections
            var combinedLights = lights.Select(light =>
            {
                var smartDevice = smartDevices.FirstOrDefault(sd => sd["_id"] == light.Id);
                if (smartDevice != null)
                {
                    light.Name = smartDevice["Name"].AsString;
                    light.Type = smartDevice["Type"].AsString;
                }
                return light;
            }).ToList();

            return Ok(combinedLights);
        }

        /// <summary>
        /// Retrieves a specific light by its ID.
        /// </summary>
        /// <param name="id">The ID of the light from the get_all_lights tool.</param>
        /// <returns>The requested light or a 404 error if not found.</returns>
        [HttpGet("{id}", Name="get_light")]
        public IActionResult GetLight(string id)
        {
            // Fetch base device information to ensure it's a light device
            var smartDevice = _smartDevices
                .Find(Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("_id", id),
                    Builders<BsonDocument>.Filter.Eq("Type", "Light")))
                .FirstOrDefault();

            if (smartDevice == null)
            {
                return NotFound();
            }

            // Fetch the corresponding light-specific data
            var light = _lights.Find(d => d.Id == id).FirstOrDefault();
            if (light == null)
            {
                return NotFound();
            }

            light.Name = smartDevice["Name"].AsString;
            light.Type = smartDevice["Type"].AsString;

            return Ok(light);
        }

        /// <summary>
        /// Changes the state of a light.
        /// </summary>
        /// <param name="id">The ID of the light to change from the get_all_lights tool.</param>
        /// <param name="newStateRequest">The new state of the light.</param>
        /// <returns>The updated light or a 404 error if not found.</returns>
        [HttpPost("{id}", Name="change_light_state")]
        public IActionResult ChangeLightState(string id, ChangeSpeakerStateRequest newStateRequest)
        {
            // Fetch base device information to ensure it's a light device
            var smartDevice = _smartDevices
                .Find(Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("_id", id),
                    Builders<BsonDocument>.Filter.Eq("Type", "Light")))
                .FirstOrDefault();

            if (smartDevice == null)
            {
                return NotFound();
            }

            // Fetch the corresponding light-specific data
            var light = _lights.Find(d => d.Id == id).FirstOrDefault();
            if (light == null)
            {
                return NotFound();
            }

            DateTime scheduledTime = newStateRequest.ScheduledTime.AddMilliseconds(LatencyBuffer);
            if (scheduledTime > DateTime.Now)
            {
                var timer = new Timer(
                    _ => light.ChangeState(
                        newStateRequest.IsOn,
                        newStateRequest.HexColor,
                        newStateRequest.Brightness,
                        newStateRequest.FadeDurationInMilliseconds),
                    null,
                    scheduledTime - DateTime.Now,
                    TimeSpan.FromMilliseconds(-1));
            }
            else
            {
                light.ChangeState(
                    newStateRequest.IsOn,
                    newStateRequest.HexColor,
                    newStateRequest.Brightness,
                    newStateRequest.FadeDurationInMilliseconds);
            }

            _lights.ReplaceOne(d => d.Id == light.Id, light);
            return Ok(light);
        }
    }
}

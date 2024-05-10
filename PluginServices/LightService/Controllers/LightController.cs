using Shared.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using LightService.Models;
using MongoDB.Bson;

namespace LightService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LightsController(IMongoDatabase database) : ControllerBase
    {
        private readonly IMongoCollection<BsonDocument> _smartDevices = database.GetCollection<BsonDocument>("SmartDevices");
        private readonly IMongoCollection<Light> _lights = database.GetCollection<Light>("Lights");
        private const int LatencyBuffer = 300; // milliseconds

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

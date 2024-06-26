using Microsoft.AspNetCore.Mvc;
using PartyPlanning.PluginServices.LightService.Models;
using Q42.HueApi;
using Q42.HueApi.ColorConverters;
using Q42.HueApi.ColorConverters.HSB;
using Q42.HueApi.ColorConverters.Original;

namespace PartyPlanning.PluginServices.LightService.Controllers
{   
    [ApiController]
    [Route("[controller]")]
    public class LightController(LocalHueClient client) : ControllerBase
    {
        const string ID_PREFIX = "xyz";

        // private readonly IMongoCollection<BsonDocument> _smartDevices = database.GetCollection<BsonDocument>("SmartDevices");
        // private readonly IMongoCollection<Light> _lights = database.GetCollection<Light>("Lights");
        private const int LATENCY_BUFFER = 10000; // milliseconds

        /// <summary>
        /// Retrieves all lights in the system.
        /// </summary>
        /// <returns>Returns the current state of the light and its 6 character long ID for other API requests</returns>
        /// <response code="200">Returns a list of lights with their current state</response>
        [HttpGet(Name="get_all_lights")]
        [ProducesResponseType(typeof(IEnumerable<LightStateModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLightsAsync()
        {   
            // Get the list of lights from the bridge
            var lights = await client.GetLightsAsync().ConfigureAwait(false);


            // Return the list of light IDs
            return Ok(lights.Select(light => {
                HSB color = new(light.State.Hue!.Value, light.State.Saturation!.Value, light.State.Brightness);
                return new LightStateModel() {
                    Id = ID_PREFIX+light.Id,
                    Name = light.Name,
                    On = light.State.On,
                    Brightness = light.State.Brightness,
                    HexColor = color.GetRGB().ToHex()
                };
            }));
        }

        /// <summary>
        /// Retrieves a specific light by its ID.
        /// </summary>
        /// <param name="id">The ID of the light from the get_all_lights tool.</param>
        /// <returns>The requested light or a 404 error if not found.</returns>
        /// <response code="200">Returns the requested light</response>
        /// <response code="404">If the light is not found</response>
        [HttpGet("{id}", Name="get_light")]
        [ProducesResponseType(typeof(LightStateModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLightAsync(string id)
        {
            // Remove prefix from the ID
            id = id[3..];

            // Get the state of the light with the specified ID
            var light = await client.GetLightAsync(id).ConfigureAwait(false);
            HSB color = new(light!.State.Hue!.Value, light.State.Saturation!.Value, light.State.Brightness);
            
            return Ok(new LightStateModel() {
                Id = light.Id,
                Name = light.Name,
                On = light.State.On,
                Brightness = light.State.Brightness,
                HexColor = color.GetRGB().ToHex()
            });
        }

        /// <summary>
        /// Changes the state of a light.
        /// </summary>
        /// <param name="id">The ID of the light to change from the get_all_lights tool.</param>
        /// <param name="changeStateRequest">The new state of the light and change parameters.</param>
        /// <returns>The updated light or a 404 error if not found.</returns>
        /// <response code="200">Returns the updated light state</response>
        /// <response code="404">If the light is not found</response>
        [HttpPost("{id}", Name="change_light_state")]
        [ProducesResponseType(typeof(LightStateModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> ChangeLightStateAsync(string id, ChangeStateRequest changeStateRequest)
        {
            // Remove prefix from the ID
            id = id[3..];

            // Get the state of the light with the specified ID
            var light = await client.GetLightAsync(id).ConfigureAwait(false);

            if (light == null)
            {
                return null;
            }

            // Check if the light has a scheduled state change
            if (changeStateRequest.ScheduledTime != null)
            {
                // Calculate the time until the scheduled state change (include a latency buffer to account for network latency)
                var timeUntilChange = Math.Max((changeStateRequest.ScheduledTime.Value - DateTime.Now + new TimeSpan(LATENCY_BUFFER)).Milliseconds, 0);

                // Wait until the scheduled time
                await Task.Delay(timeUntilChange).ConfigureAwait(false);
            }   

            if (changeStateRequest.HexColor != null)
            {
                RGBColor? rGBColor = new(changeStateRequest.HexColor);

                // Send the updated state to the light
                await client.SendCommandAsync(new LightCommand
                {
                    On = changeStateRequest.IsOn ?? light.State.On,
                    Brightness = changeStateRequest.Brightness ?? light.State.Brightness,
                    Hue = (int?)rGBColor?.GetHue(),
                    Saturation = (int?)rGBColor?.GetSaturation()
                }, [id]).ConfigureAwait(false);
            } else
            {
                // Send the updated state to the light
                await client.SendCommandAsync(new LightCommand
                {
                    On = changeStateRequest.IsOn ?? light.State.On,
                    Brightness = changeStateRequest.Brightness ?? light.State.Brightness
                }, [id]).ConfigureAwait(false);
            }

            // Return the updated state of the light
            return await GetLightAsync(ID_PREFIX+id);
        }
    }
}

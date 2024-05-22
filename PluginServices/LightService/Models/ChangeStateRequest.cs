using System.ComponentModel;

namespace PartyPlanning.PluginServices.LightService.Models
{
    /// <summary>
    /// Represents a request to change the state of the light.
    /// </summary>
    public class ChangeStateRequest
    {
        /// <summary>
        /// Specifies whether the light is turned on or off.
        /// </summary>
        public bool? IsOn { get; set; }

        /// <summary>
        /// The hex color code for the light.
        /// </summary>
        public string? HexColor { get; set; }

        /// <summary>
        /// The brightness level of the light.
        /// </summary>
        public byte? Brightness { get; set; }

        /// <summary>
        /// Duration for the light to fade to the new state, in milliseconds.
        /// </summary>
        public int? FadeDurationInMilliseconds { get; set; } = 100;

        /// <summary>
        /// Use ScheduledTime to synchronize lights. It's recommended that you asynchronously create tasks for each light that's scheduled to avoid blocking the main thread.
        /// </summary>
        public DateTime? ScheduledTime { get; set; }
    }
}

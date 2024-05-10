using Shared.Enums;

namespace LightService.Models
{
    /// <summary>
    /// Represents a request to change the state of the light.
    /// </summary>
    public class ChangeSpeakerStateRequest
    {
        /// <summary>
        /// Specifies whether the light is turned on or off.
        /// </summary>
        public bool IsOn { get; set; }

        /// <summary>
        /// The hex color code for the light.
        /// </summary>
        public string? HexColor { get; set; }

        /// <summary>
        /// The brightness level of the light.
        /// </summary>
        public Brightness Brightness { get; set; }

        /// <summary>
        /// Duration for the light to fade to the new state, in milliseconds.
        /// </summary>
        public int FadeDurationInMilliseconds { get; set; } = 100;

        /// <summary>
        /// The time at which the change should occur.
        /// </summary>
        public DateTime ScheduledTime { get; set; } = DateTime.Now;
    }
}

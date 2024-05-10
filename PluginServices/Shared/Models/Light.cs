using Shared.Enums;

namespace Shared.Models
{
    /// <summary>
    /// Represents a light with various properties.
    /// </summary>
    public class Light(string name) : SmartDevice(name, "Light")
    {
        /// <summary>
        /// Specifies whether the light is turned on or off.
        /// </summary>
        public bool IsOn { get; private set; } = false;

        /// <summary>
        /// The hex color code for the light.
        /// </summary>
        public string HexColor { get; private set; } = "#FFFFFF";

        /// <summary>
        /// The brightness level of the light.
        /// </summary>
        public Brightness Brightness { get; private set; } = Brightness.Medium;

        /// <summary>
        /// Changes the state of the light.
        /// </summary>
        /// <param name="isOn">Specifies whether the light is turned on or off.</param>
        /// <param name="hexColor">The hex color code for the light.</param>
        /// <param name="brightness">The brightness level of the light.</param>
        /// <param name="fadeDurationInMilliseconds">Duration for the light to fade to the new state, in milliseconds.</param>
        public void ChangeState(
            bool? isOn,
            string? hexColor,
            Brightness? brightness,
            int? fadeDurationInMilliseconds)
        {
            IsOn = isOn ?? IsOn;
            HexColor = hexColor ?? HexColor;
            Brightness = brightness ?? Brightness;
        }
    }
}

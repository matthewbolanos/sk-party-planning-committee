using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Shared.Enums
{
    /// <summary>
    /// Describes the brightness levels of the light.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Brightness
    {
        /// <summary>
        /// Low brightness level.
        /// </summary>
        [Description("Low brightness level.")]
        Low,

        /// <summary>
        /// Medium brightness level.
        /// </summary>
        [Description("Medium brightness level.")]
        Medium,

        /// <summary>
        /// High brightness level.
        /// </summary>
        [Description("High brightness level.")]
        High
    }
}

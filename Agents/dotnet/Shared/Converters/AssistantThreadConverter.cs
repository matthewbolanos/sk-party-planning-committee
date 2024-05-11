using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel.ChatCompletion;
using Shared.Models;

namespace Shared.Converters
{
    /// <summary>
    /// Converts JSON to <see cref="AssistantThread"/> and vice versa.
    /// </summary>
    public class AssistantThreadConverter : JsonConverter<AssistantThread>
    {
        /// <summary>
        /// Represents the content of a thread message.
        /// </summary>
        public override AssistantThread Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Perform default deserialization
            var AssistantThread = JsonSerializer.Deserialize<AssistantThread>(ref reader, options);

            return AssistantThread!;
        }

        /// <summary>
        /// Writes the JSON representation of a <see cref="AssistantThread"/> object to a <see cref="Utf8JsonWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="Utf8JsonWriter"/> to write the JSON to.</param>
        /// <param name="value">The <see cref="AssistantThread"/> object to serialize.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> used during serialization.</param>
        public override void Write(Utf8JsonWriter writer, AssistantThread value, JsonSerializerOptions options)
        {
            // Perform serialization with the base class
            JsonSerializer.Serialize<AssistantThreadBase>(writer, value, options);
        }
    }
}


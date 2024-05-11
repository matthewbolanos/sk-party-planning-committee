using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Shared.Models;

namespace Shared.Converters
{
    /// <summary>
    /// Converts JSON to <see cref="AssistantMessageContent"/> and vice versa.
    /// </summary>
    public class AssistantMessageContentConverter : JsonConverter<AssistantMessageContent>
    {
        /// <summary>
        /// Represents the content of a thread message.
        /// </summary>
        public override AssistantMessageContent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Deserialize JSON to a dictionary
            var jsonObject = JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options);

            if (jsonObject == null)
            {
                throw new JsonException();
            }

            // Extract values from dictionary
            var id = (string)jsonObject["id"];
            var threadId = (string?)jsonObject["thread_id"];
            var createdAt = ((JsonElement)jsonObject["created_at"]).GetDateTime();
            var role = Enum.Parse<AuthorRole>((string)jsonObject["role"]);
            var items = JsonSerializer.Deserialize<List<KernelContent>>(((JsonElement)jsonObject["content"]).GetString()!, options); // improve?
            var assistantId = (string?)jsonObject["assistant_id"];
            var runId = (string?)jsonObject["run_id"];

            // Create a new instance of AssistantMessageContent
            var AssistantMessageContent = new AssistantMessageContent()
            {
                Role = role,
                Items = [.. items],
                Id = id,
                ThreadId = threadId,
                CreatedAt = createdAt,
                AssistantId = assistantId,
                RunId = runId
            };

            return AssistantMessageContent;
        }

        /// <summary>
        /// Writes the JSON representation of a <see cref="AssistantMessageContent"/> object to a <see cref="Utf8JsonWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="Utf8JsonWriter"/> to write the JSON to.</param>
        /// <param name="value">The <see cref="AssistantMessageContent"/> object to serialize.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> used during serialization.</param>
        public override void Write(Utf8JsonWriter writer, AssistantMessageContent value, JsonSerializerOptions options)
        {
            // Serialize AssistantMessageContent to JSON
            writer.WriteStartObject();
            writer.WriteString("id", value.Id);
            writer.WriteString("object", "thread.message");
            writer.WriteNumber("created_at", new DateTimeOffset(value.CreatedAt).ToUnixTimeSeconds());
            writer.WriteString("assistant_id", value.AssistantId);
            writer.WriteString("thread_id", value.ThreadId);
            writer.WriteString("run_id", value.RunId);
            writer.WriteString("role", value.Role.ToString().ToLower());

            // Write the 'content' array using the custom converter
            JsonSerializer.Serialize(writer, value.Items, new JsonSerializerOptions
            {
                Converters = { new ListOfKernelContentConverter() }
            });

            writer.WriteEndObject();
        }
    }
}

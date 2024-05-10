using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Shared.Models;

namespace Shared.Converters
{
    /// <summary>
    /// Converts JSON to <see cref="ThreadMessageContent"/> and vice versa.
    /// </summary>
    public class ThreadMessageContentConverter : JsonConverter<ThreadMessageContent>
    {
        /// <summary>
        /// Represents the content of a thread message.
        /// </summary>
        public override ThreadMessageContent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
            var content = ((JsonElement)jsonObject["content"]).GetString();

            // Create a new instance of ThreadMessageContent
            var threadMessageContent = new ThreadMessageContent()
            {
                Role = role,
                Content = content,
                Id = id,
                ThreadId = threadId,
                CreatedAt = createdAt
            };

            return threadMessageContent;
        }

        /// <summary>
        /// Writes the JSON representation of a <see cref="ThreadMessageContent"/> object to a <see cref="Utf8JsonWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="Utf8JsonWriter"/> to write the JSON to.</param>
        /// <param name="value">The <see cref="ThreadMessageContent"/> object to serialize.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> used during serialization.</param>
        public override void Write(Utf8JsonWriter writer, ThreadMessageContent value, JsonSerializerOptions options)
        {
            // Serialize ThreadMessageContent to JSON
            writer.WriteStartObject();
            writer.WriteString("id", value.Id);
            writer.WriteString("object", "thread.message");
            writer.WriteNumber("created_at", new DateTimeOffset(value.CreatedAt).ToUnixTimeSeconds());
            writer.WriteNull("assistant_id"); // You can add logic here if needed
            writer.WriteString("thread_id", value.ThreadId);
            writer.WriteNull("run_id"); // You can add logic here if needed
            writer.WriteString("role", value.Role.ToString().ToLower());
            writer.WriteStartArray("content");
            writer.WriteStartObject();
            writer.WriteString("type", "text");
            writer.WriteStartObject("text");
            writer.WriteString("value", value.Content);
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}

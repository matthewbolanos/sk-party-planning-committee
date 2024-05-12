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
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token.");
            }

            string? id = null, threadId = null, assistantId = null, runId = null;
            List<KernelContent>? items = null;
            DateTime? createdAt = null;
            AuthorRole? role = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read(); // Move to the value token

                    switch (propertyName)
                    {
                        case "id":
                            id = reader.GetString();
                            break;
                        case "thread_id":
                            threadId = reader.GetString();
                            break;
                        case "created_at":
                            // Convert Unix timestamp to DateTime
                            createdAt = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64()).DateTime;
                            break;
                        case "role":
                            string? roleLabel = reader.GetString();
                            if (roleLabel != null)
                            {
                                role = new AuthorRole(roleLabel);
                            }
                            else
                            {
                                throw new JsonException("Role is missing.");
                            }
                            break;
                        case "content":
                            string? contentJson = reader.GetString();
                            if (contentJson != null)
                            {
                                // Check if contentJson is actually json or a string that doesn't need to be deserialized
                                if (contentJson.StartsWith('['))
                                {
                                    // Deserialize the content array
                                    items = JsonSerializer.Deserialize<List<KernelContent>>(contentJson, options);
                                }
                                else
                                {
                                    // If contentJson is a string, create a new KernelContent object with the string as the text
                                    items =
                                    [
                                        new TextContent
                                        {
                                            Text = contentJson
                                        }
                                    ];
                                }
                            }
                            break;
                        case "assistant_id":
                            assistantId = reader.GetString();
                            break;
                        case "run_id":
                            runId = reader.GetString();
                            break;
                        default:
                            reader.Skip(); // Skip unknown properties
                            break;
                    }
                }
            }

            if (role == null || items == null)
            {
                throw new JsonException("Required properties are missing.");
            }

            var assistantMessageContent = new AssistantMessageContent()
            {
                Role = role.Value,
                Items = [.. items],
                ThreadId = threadId,
                AssistantId = assistantId,
                RunId = runId
            };

            if (id != null)
            {
                assistantMessageContent.Id = id;
            }
            if (createdAt != null)
            {
                assistantMessageContent.CreatedAt = createdAt.Value;
            }

            return assistantMessageContent;
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

            writer.WritePropertyName("content");
            JsonSerializer.Serialize<List<KernelContent>>(writer: writer, value: [.. value.Items], options: options);
            writer.WriteStartArray("attachments");
            writer.WriteEndArray();
            writer.WriteStartObject("metadata");
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
    }
}

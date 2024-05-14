using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using Shared.Models;

public class AssistantThreadConverter : JsonConverter<AssistantThreadBase>
{
    public override AssistantThreadBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject token type");

        var thread = new AssistantThreadBase();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return thread;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();
                switch (propertyName)
                {
                    case "id":
                        thread.Id = reader.GetString()!;
                        break;
                    case "object":
                        thread.Object = reader.GetString()!;
                        break;
                    case "created_at":
                        var unixTime = reader.GetInt64();
                        thread.CreatedAt = DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
                        break;
                    default:
                        throw new JsonException($"Property {propertyName} is not supported");
                }
            }
        }

        throw new JsonException("Unexpected end when reading JSON.");
    }

    public override void Write(Utf8JsonWriter writer, AssistantThreadBase value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("id", value.Id);
        writer.WriteString("object", value.Object);
        var unixTime = new DateTimeOffset(value.CreatedAt).ToUnixTimeSeconds();
        writer.WriteNumber("created_at", unixTime);
        writer.WriteStartObject("tool_resources");
        writer.WriteNull("code_interpreter");
        writer.WriteNull("file_search");
        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}

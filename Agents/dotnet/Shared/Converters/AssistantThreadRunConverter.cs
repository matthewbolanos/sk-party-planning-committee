using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.Models;

public class AssistantThreadRunConverter : JsonConverter<AssistantThreadRun>
{
    public override AssistantThreadRun Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject token type");

        var run = new AssistantThreadRun();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return run;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();
                    
                switch (propertyName)
                {
                    case "id":
                        run.Id = reader.GetString()!;
                        break;
                    case "threadId":
                        run.ThreadId = reader.GetString()!;
                        break;
                    case "assistant_id":
                        run.AssistantId = reader.GetString()!;
                        break;
                    case "model":
                        run.Model = reader.GetString()!;
                        break;
                    case "stream":
                        run.Stream = reader.GetBoolean()!;
                        break;
                    case "created_at":
                        var unixTime = reader.GetInt64()!;
                        run.CreatedAt = DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
                        break;
                    default:
                        throw new JsonException($"Property {propertyName} is not supported");
                }
            }
        }

        throw new JsonException("Unexpected end when reading JSON.");
    }

    public override void Write(Utf8JsonWriter writer, AssistantThreadRun value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("id", value.Id);
        if (value.ThreadId != null)
            writer.WriteString("threadId", value.ThreadId);
        if (value.AssistantId != null)
            writer.WriteString("assistant_id", value.AssistantId);
        if (value.Model != null)
            writer.WriteString("model", value.Model);
        writer.WriteBoolean("stream", value.Stream);
        var unixTime = new DateTimeOffset(value.CreatedAt).ToUnixTimeSeconds();
        writer.WriteNumber("created_at", unixTime);
        writer.WriteEndObject();
    }
}

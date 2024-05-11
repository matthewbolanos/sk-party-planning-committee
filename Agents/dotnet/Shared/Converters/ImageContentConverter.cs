using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;

public class ImageContentConverter : JsonConverter<ImageContent>
{
    public override ImageContent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject token");

        string url = "";

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();  // Move to property value

                if (propertyName == "url")
                {
                    url = reader.GetString()!;
                }
            }
        }

        return new ImageContent { Uri = new Uri(url) };
    }

    public override void Write(Utf8JsonWriter writer, ImageContent value, JsonSerializerOptions options)
    {
        if (value.Uri == null)
            throw new JsonException("Image URL is required");

        writer.WriteStartObject();
        writer.WriteString("type", "image_url");
        writer.WritePropertyName("image_url");
        writer.WriteStartObject();
        writer.WriteString("url", value.Uri.ToString());
        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}

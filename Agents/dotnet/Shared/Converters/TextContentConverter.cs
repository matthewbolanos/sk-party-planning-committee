using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;

public class TextContentConverter : JsonConverter<TextContent>
{
    public override TextContent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject token");

        string text = "";
        List<string> annotations = [];

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read(); // Move to property value

                switch (propertyName)
                {
                    case "value":
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            text = reader.GetString()!;
                        }
                        else if (reader.TokenType == JsonTokenType.StartObject)
                        {
                            reader.Read(); // Read the property name inside the object
                            reader.Read(); // Read the value inside the object
                            text = reader.GetString()!;
                            reader.Read(); // End object
                        }
                        break;
                    case "annotations":
                        if (reader.TokenType == JsonTokenType.StartArray)
                        {
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                            {
                                annotations.Add(reader.GetString()!);
                            }
                        }
                        break;
                }
            }
        }

        return new TextContent { Text = text };
    }

    public override void Write(Utf8JsonWriter writer, TextContent value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "text");
        writer.WritePropertyName("text");
        writer.WriteStartObject();
        writer.WriteString("value", value.Text);
        writer.WritePropertyName("annotations");
        writer.WriteStartArray();
        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}

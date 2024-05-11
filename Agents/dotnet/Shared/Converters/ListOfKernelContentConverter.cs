using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;

namespace Shared.Converters
{
    public class ListOfKernelContentConverter : JsonConverter<List<KernelContent>>
    {
        public override List<KernelContent> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected StartArray token");

            var contents = new List<KernelContent>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    reader.Read(); // Move to property name, assuming "type"
                    reader.GetString(); // Skip "type"
                    reader.Read(); // Move to property value
                    var type = reader.GetString();

                    switch (type)
                    {
                        case "text":
                            reader.Read(); // Move to next property name
                            reader.GetString(); // Skip property name, assuming "text"
                            reader.Read(); // Move to property value
                            if (reader.TokenType == JsonTokenType.String)
                            {
                                var text = reader.GetString();
                                contents.Add(new TextContent { Text = text });
                                reader.Read(); // Skip EndObject
                            }
                            else if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                reader.Read(); // Property name (assuming "value")
                                reader.Read(); // Property value
                                var text = reader.GetString();
                                reader.Read(); // End object
                                contents.Add(new TextContent { Text = text });
                            }
                            break;
                        case "image_url":
                            reader.Read(); // Move to next property name
                            reader.GetString(); // Skip property name, assuming "url"
                            reader.Read(); // Move to property value
                            var url = reader.GetString();
                            contents.Add(new ImageContent { Uri = new Uri(url!) });
                            reader.Read(); // Skip EndObject
                            break;
                        default:
                            throw new JsonException($"Unexpected content type {type}");
                    }
                }
            }

            return contents;
        }

        public override void Write(Utf8JsonWriter writer, List<KernelContent> contents, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var content in contents)
            {
                writer.WriteStartObject();
                switch (content)
                {
                    case TextContent textContent:
                        writer.WriteString("type", "text");
                        writer.WriteString("text", textContent.Text);
                        break;
                    case ImageContent imageContent:
                        if(imageContent.Uri == null)
                        {
                            throw new JsonException("Image URL is required");
                        }
                        writer.WriteString("type", "image_url");
                        writer.WriteString("url", imageContent.Uri.ToString());
                        break;
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
    }
}

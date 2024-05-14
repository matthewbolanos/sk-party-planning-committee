using System;
using System.Collections.Generic;
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
                    using (var jsonDoc = JsonDocument.ParseValue(ref reader))
                    {
                        var jsonObject = jsonDoc.RootElement;
                        var type = jsonObject.GetProperty("type").GetString();

                        KernelContent content = type switch
                        {
                            "text" => JsonSerializer.Deserialize<TextContent>(jsonObject.GetRawText(), options)!,
                            "image_url" => JsonSerializer.Deserialize<ImageContent>(jsonObject.GetRawText(), options)!,
                            #pragma warning disable SKEXP0001
                            "functionCall" => JsonSerializer.Deserialize<FunctionCallContent>(jsonObject.GetRawText(), options)!,
                            "functionResult" => JsonSerializer.Deserialize<FunctionResultContent>(jsonObject.GetRawText(), options)!,
                            #pragma warning restore SKEXP0001
                            _ => throw new JsonException($"Unexpected content type {type}")
                        };

                        contents.Add(content);
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
                        writer.WritePropertyName("text");
                        JsonSerializer.Serialize(writer, textContent, options);
                        break;
                    case ImageContent imageContent:
                        writer.WriteString("type", "image_url");
                        writer.WritePropertyName("image_url");
                        JsonSerializer.Serialize(writer, imageContent, options);
                        break;
                    #pragma warning disable SKEXP0001
                    case FunctionCallContent functionCallContent:
                        writer.WriteString("type", "functionCall");
                        writer.WritePropertyName("functionCall");
                        JsonSerializer.Serialize(writer, functionCallContent, options);
                        break;
                    case FunctionResultContent functionResultContent:
                        writer.WriteString("type", "functionResult");
                        writer.WritePropertyName("functionResult");
                        JsonSerializer.Serialize(writer, functionResultContent, options);
                        break;
                    #pragma warning restore SKEXP0001
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;

#pragma warning disable SKEXP0001

public class FunctionCallContentConverter : JsonConverter<FunctionCallContent>
{
    public override FunctionCallContent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject token");

        string pluginName = string.Empty;
        string functionName = string.Empty;
        string id = string.Empty;
        Dictionary<string, object?> arguments = new();

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read(); // Move to property value

                switch (propertyName)
                {
                    case "pluginName":
                        pluginName = reader.GetString()!;
                        break;
                    case "functionName":
                        functionName = reader.GetString()!;
                        break;
                    case "id":
                        id = reader.GetString()!;
                        break;
                    case "arguments":
                        arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options)!;
                        break;
                }
            }
        }

        return new FunctionCallContent(
            pluginName: pluginName,
            functionName: functionName,
            id: id,
            arguments: new KernelArguments(arguments)
        );
    }

    public override void Write(Utf8JsonWriter writer, FunctionCallContent value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "functionCall");
        writer.WritePropertyName("functionCall");
        writer.WriteStartObject();
        writer.WriteString("pluginName", value.PluginName);
        writer.WriteString("functionName", value.FunctionName);
        writer.WriteString("id", value.Id);
        writer.WritePropertyName("arguments");
        JsonSerializer.Serialize(writer, value.Arguments, options);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}

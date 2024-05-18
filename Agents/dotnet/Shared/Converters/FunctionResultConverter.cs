using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;

#pragma warning disable SKEXP0001

public class FunctionResultContentConverter : JsonConverter<FunctionResultContent>
{
    public override FunctionResultContent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject token");

        string pluginName = string.Empty;
        string functionName = string.Empty;
        string id = string.Empty;
        string? result = null;

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
                    case "result":
                        result = reader.GetString()!;
                        break;
                }
            }
        }

        return new FunctionResultContent(
            pluginName: pluginName,
            functionName: functionName,
            id: id,
            result: result
        );
    }

    public override void Write(Utf8JsonWriter writer, FunctionResultContent value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "functionResult");
        writer.WritePropertyName("functionResult");
        writer.WriteStartObject();
        writer.WriteString("pluginName", value.PluginName);
        writer.WriteString("functionName", value.FunctionName);
        writer.WriteString("id", value.Id);
        writer.WriteString("result", (string)value.Result!);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}


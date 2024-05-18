using System.Text.Json;
using System.Text.Json.Serialization;


namespace PartyPlanning.Agents.Plugins.PythonInterpreter;

public class PythonInterpreterExecutionResultResultConverter : JsonConverter<PythonInterpreterExecutionResult>
{
    public override PythonInterpreterExecutionResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token");
        }

        var response = new PythonInterpreterExecutionResult();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return response;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read(); // Move to the value

                switch (propertyName)
                {
                    case "status":
                        response.Status = reader.GetString();
                        break;
                    case "stdout":
                        response.Stdout = reader.GetString();
                        break;
                    case "stderr":
                        response.Stderr = reader.GetString();
                        break;
                    case "result":
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            response.Result = reader.GetString();
                        }
                        else if (reader.TokenType == JsonTokenType.Number)
                        {
                            response.Result = reader.GetDouble().ToString(); // Or use GetInt32() based on your data
                        }
                        // Add more cases here for different types as needed
                        break;
                    case "executionTimeInMilliseconds":
                        response.ExecutionTimeInMilliseconds = reader.GetInt32();
                        break;
                }
            }
        }

        throw new JsonException("Expected EndObject token");
    }

    public override void Write(Utf8JsonWriter writer, PythonInterpreterExecutionResult value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (!string.IsNullOrEmpty(value.Stdout))
        {
            writer.WriteString("stdout", value.Stdout);
        }

        if (!string.IsNullOrEmpty(value.Stderr))
        {
            writer.WriteString("stderr", value.Stderr);
        }

        if (!string.IsNullOrEmpty(value.Result))
        {
            writer.WriteString("result", value.Result);
        }

        writer.WriteEndObject();
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(PythonInterpreterExecutionResult);
    }
}

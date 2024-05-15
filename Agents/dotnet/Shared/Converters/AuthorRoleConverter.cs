using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel.ChatCompletion;

namespace PartyPlanning.Agents.Shared.Converters
{
    public class AuthorRoleConverter : JsonConverter<AuthorRole>
    {
        public override AuthorRole Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Unexpected token type: {reader.TokenType}");
            }

            string label = reader.GetString()!;
            return new AuthorRole(label.ToLowerInvariant());
        }

        public override void Write(Utf8JsonWriter writer, AuthorRole value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Label.ToLowerInvariant());
        }
    }
}

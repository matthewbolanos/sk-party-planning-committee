using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Microsoft.SemanticKernel.ChatCompletion;

public class AuthorRoleSerializer : SerializerBase<AuthorRole>
{
    public override AuthorRole Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var label = context.Reader.ReadString();
        switch (label)
        {
            case "user":
                return AuthorRole.User;
            case "assistant":
                return AuthorRole.Assistant;
            case "system":
                return AuthorRole.System;
            case "tool":
                return AuthorRole.Tool;
            default:                
                throw new Exception($"Unknown role: {label}");
        }
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, AuthorRole value)
    {
        context.Writer.WriteString(value.Label);
    }
}

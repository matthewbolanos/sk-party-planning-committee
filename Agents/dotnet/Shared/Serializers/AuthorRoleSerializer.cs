using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Microsoft.SemanticKernel.ChatCompletion;

public class AuthorRoleSerializer : SerializerBase<AuthorRole>
{
    public override AuthorRole Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var label = context.Reader.ReadString();
        return new AuthorRole(label);
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, AuthorRole value)
    {
        context.Writer.WriteString(value.Label);
    }
}

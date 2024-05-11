using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Microsoft.SemanticKernel;
using System;

namespace Shared.Serializers
{
    public class ImageContentSerializer : IBsonSerializer<ImageContent>
    {
    public Type ValueType => typeof(ImageContent);

    public ImageContent Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var doc = BsonDocumentSerializer.Instance.Deserialize(context, args);
        var url = doc["image_url"]["url"].AsString;
        return new ImageContent { Uri = new Uri(url) };
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ImageContent value)
    {
        if (value.Uri == null)
            throw new InvalidOperationException("Image URL is required for serialization.");

        var doc = new BsonDocument
        {
            { "type", "image_url" },
            { "image_url", new BsonDocument { { "url", value.Uri.ToString() } } }
        };

        BsonDocumentSerializer.Instance.Serialize(context, doc);
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        Serialize(context, args, (ImageContent)value);
    }

    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return Deserialize(context, args);
    }
}

}

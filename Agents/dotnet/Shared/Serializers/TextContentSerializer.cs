using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;

namespace Shared.Serializers
{
    public class TextContentSerializer : IBsonSerializer<TextContent>
    {
        public Type ValueType => typeof(TextContent);

        public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var doc = BsonDocumentSerializer.Instance.Deserialize(context, args);
            var text = doc["text"].AsString;
            var annotations = new List<string>();
            return new TextContent { Text = text };
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TextContent value)
        {
            var doc = new BsonDocument
            {
                { "type", "text" },
                { "text", value.Text }
            };

            var annotationsArray = new BsonArray();
            doc.Add("annotations", annotationsArray);

            BsonDocumentSerializer.Instance.Serialize(context, doc);
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            Serialize(context, args, (TextContent)value);
        }

        TextContent IBsonSerializer<TextContent>.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return (TextContent)Deserialize(context, args);
        }
    }
}
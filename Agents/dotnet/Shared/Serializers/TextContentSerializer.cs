using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Shared.Serializers
{
    public class TextContentSerializer : IBsonSerializer<TextContent>
    {
        public Type ValueType => typeof(TextContent);

        public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var doc = BsonDocumentSerializer.Instance.Deserialize(context, args);
            var text = doc["text"].AsBsonDocument["value"].AsString;
            return new TextContent { Text = text };
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TextContent value)
        {
            var doc = new BsonDocument
            {
                { "type", "text" },
                { "text", new BsonDocument{
                    { "value", value.Text },
                    { "annotations", new BsonArray() }
                } }
            };

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
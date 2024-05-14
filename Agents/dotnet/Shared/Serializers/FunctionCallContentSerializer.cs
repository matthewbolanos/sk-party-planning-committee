using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Text.Json;

#pragma warning disable SKEXP0001

namespace Shared.Serializers
{
    public class FunctionCallContentSerializer : IBsonSerializer<FunctionCallContent>
    {
        public Type ValueType => typeof(FunctionCallContent);

        public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var doc = BsonDocumentSerializer.Instance.Deserialize(context, args);

            var functionCallDoc = doc["functionCall"].AsBsonDocument;

            var functionCallContent = new FunctionCallContent(
                pluginName: functionCallDoc["pluginName"].AsString,
                functionName: functionCallDoc["functionName"].AsString,
                id: functionCallDoc["id"].AsString,
                arguments: new KernelArguments(JsonSerializer.Deserialize<Dictionary<string, object>>(functionCallDoc["arguments"].AsString)!)
            );

            return functionCallContent;
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, FunctionCallContent value)
        {
            var doc = new BsonDocument
            {
                { "type", "functionCall" },
                { "functionCall", new BsonDocument{
                    { "pluginName", value.PluginName },
                    { "functionName", value.FunctionName },
                    { "id", value.Id },
                    { "arguments", JsonSerializer.Serialize(value.Arguments) }
                } }
            };

            BsonDocumentSerializer.Instance.Serialize(context, doc);
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            Serialize(context, args, (FunctionCallContent)value);
        }

        FunctionCallContent IBsonSerializer<FunctionCallContent>.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return (FunctionCallContent)Deserialize(context, args);
        }
    }
}
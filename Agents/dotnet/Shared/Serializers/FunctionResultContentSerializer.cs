using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Microsoft.SemanticKernel;
using System.Text.Json;
using System.Text.Json.Nodes;

#pragma warning disable SKEXP0001

namespace Shared.Serializers
{
    public class FunctionResultContentSerializer : IBsonSerializer<FunctionResultContent>
    {
        public Type ValueType => typeof(FunctionResultContent);

        public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var doc = BsonDocumentSerializer.Instance.Deserialize(context, args);

            var functionResultDoc = doc["functionResult"].AsBsonDocument;

            var functionResultContent = new FunctionResultContent(
                pluginName: functionResultDoc["pluginName"].AsString,
                functionName: functionResultDoc["functionName"].AsString,
                id: functionResultDoc["id"].AsString,
                result: functionResultDoc["result"].AsString
            );

            return functionResultContent;
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, FunctionResultContent value)
        {
            var doc = new BsonDocument
            {
                { "type", "functionResult" },
                { "functionResult", new BsonDocument{
                    { "pluginName", value.PluginName },
                    { "functionName", value.FunctionName },
                    { "id", value.Id },
                    { "result", (string)JsonSerializer.Deserialize<JsonObject>((string)value.Result!)!["Content"]! }
                } }
            };

            BsonDocumentSerializer.Instance.Serialize(context, doc);
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            Serialize(context, args, (FunctionResultContent)value);
        }

        FunctionResultContent IBsonSerializer<FunctionResultContent>.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return (FunctionResultContent)Deserialize(context, args);
        }
    }
}
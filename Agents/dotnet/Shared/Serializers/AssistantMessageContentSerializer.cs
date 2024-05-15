using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using PartyPlanning.Agents.Shared.Models;

namespace PartyPlanning.Agents.Shared.Serializers
{
    public class AssistantMessageContentSerializer : SerializerBase<AssistantMessageContent>, IBsonSerializer<AssistantMessageContent>, IBsonDocumentSerializer
    {
        public Type ValueType => typeof(AssistantMessageContent);

        public AssistantMessageContent Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var doc = BsonDocumentSerializer.Instance.Deserialize(context, args);

            var id = doc["_id"].AsObjectId.ToString();
            var threadId = doc.GetValue("thread_id", null)?.AsString;
            var createdAt = doc["created_at"].ToUniversalTime();
            var role = BsonSerializer.Deserialize<AuthorRole>('"'+doc["role"].AsString+'"');
            
            var items = new List<KernelContent>();
            foreach (var bson in doc["content"].AsBsonArray)
            {
                // Deserialize the content based on the type
                var type = bson["type"].AsString;
                switch(type)
                {
                    case "text":
                        items.Add(BsonSerializer.Deserialize<TextContent>(bson.AsBsonDocument));
                        break;
                    case "image":
                        items.Add(BsonSerializer.Deserialize<ImageContent>(bson.AsBsonDocument));
                        break;
                    #pragma warning disable SKEXP0001
                    case "functionCall":
                        items.Add(BsonSerializer.Deserialize<FunctionCallContent>(bson.AsBsonDocument));
                        break;
                    case "functionResult":
                        items.Add(BsonSerializer.Deserialize<FunctionResultContent>(bson.AsBsonDocument));
                        break;
                    #pragma warning restore SKEXP0001
                    default:
                        throw new Exception($"Unknown content type: {type}");
                }
            }
            
            var assistantId = doc.GetValue("assistant_id", "lighting-agent")?.AsString;
            var runId = doc.GetValue("run_id", null)?.AsString;

            return new AssistantMessageContent
            {
                Id = id,
                ThreadId = threadId,
                CreatedAt = createdAt,
                Role = role,
                Items = [.. items],
                AssistantId = assistantId,
                RunId = runId
            };
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, AssistantMessageContent value)
        {
            var doc = new BsonDocument
            {
                { "_id", new ObjectId(value.Id) },
                { "thread_id", value.ThreadId },
                { "created_at", value.CreatedAt },
                { "role", value.Role.Label }
            };

            if (value.AssistantId != null)
            {
                doc.Add("assistant_id", value.AssistantId);
            }
            if (value.RunId != null)
            {
                doc.Add("run_id", value.RunId);
            }

            var itemsArray = new BsonArray();
            foreach (var item in value.Items)
            {
                var writer = new BsonDocumentWriter([]);
                BsonSerializer.Serialize(writer, item.GetType(), item);
                itemsArray.Add(writer.Document);
            }

            doc.Add("content", itemsArray);

            BsonDocumentSerializer.Instance.Serialize(context, doc);
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            if (value is AssistantMessageContent content)
            {
                Serialize(context, args, content);
            }
            else
            {
                throw new ArgumentException("Argument must be of type AssistantMessageContent", nameof(value));
            }
        }

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            switch (memberName)
            {
                case "Id":
                    serializationInfo = new BsonSerializationInfo("_id", new StringSerializer(), typeof(string));
                    return true;
                case "ThreadId":
                    serializationInfo = new BsonSerializationInfo("thread_id", new StringSerializer(), typeof(string));
                    return true;
                case "CreatedAt":
                    serializationInfo = new BsonSerializationInfo("created_at", new DateTimeSerializer(), typeof(DateTime));
                    return true;
                case "Role":
                    serializationInfo = new BsonSerializationInfo("role", new AuthorRoleSerializer(), typeof(AuthorRole));
                    return true;
                case "Items":
                    serializationInfo = new BsonSerializationInfo("content", new ArraySerializer<KernelContent>(), typeof(IEnumerable<KernelContent>));
                    return true;
                case "AssistantId":
                    serializationInfo = new BsonSerializationInfo("assistant_id", new StringSerializer(), typeof(string));
                    return true;
                case "RunId":
                    serializationInfo = new BsonSerializationInfo("run_id", new StringSerializer(), typeof(string));
                    return true;
                default:
                    serializationInfo = null;
                    return false;
            }
        }


        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return Deserialize(context, args);
        }
    }
}

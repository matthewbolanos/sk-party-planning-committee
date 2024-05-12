using Microsoft.SemanticKernel;
using MongoDB.Bson.Serialization;
using Shared.Models;
using Shared.Serializers;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Shared.Utilities
{
    public static class MongoDBUtility
    {
        public static void ConfigureMongoDB(IServiceCollection services, string connectionString)
        {
            // Register class maps
            RegisterClassMaps();

            // Register custom serializers
            RegisterCustomSerializers();

            // MongoDB client setup
            services.AddSingleton<IMongoClient, MongoClient>(_ => new MongoClient(connectionString));
            services.AddSingleton(provider => provider.GetRequiredService<IMongoClient>().GetDatabase("PartyPlanning"));
        }

        public static void RegisterClassMaps()
        {
            BsonClassMap.RegisterClassMap<KernelContent>(cm =>
            {
                cm.AutoMap();
                cm.SetIsRootClass(true);
            });

            BsonClassMap.RegisterClassMap<TextContent>(cm =>
            {
                cm.AutoMap();
                cm.SetDiscriminator("Text");
            });

            BsonClassMap.RegisterClassMap<ImageContent>(cm =>
            {
                cm.AutoMap();
                cm.SetDiscriminator("Image");
            });
        }

        public static void RegisterCustomSerializers()
        {
            BsonSerializer.RegisterSerializer(new TextContentSerializer());
            BsonSerializer.RegisterSerializer(new ImageContentSerializer());
            BsonSerializer.RegisterSerializer(new AssistantMessageContentSerializer());
            BsonSerializer.RegisterSerializer(new AuthorRoleSerializer());
        }
    }
}
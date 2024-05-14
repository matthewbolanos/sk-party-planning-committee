using Microsoft.SemanticKernel;
using MongoDB.Bson.Serialization;
using Shared.Models;
using Shared.Serializers;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Shared.Config
{
    public static class IHostApplicationBuilderExtensions
    {
        public static IHostApplicationBuilder ConfigureMongoDB(this IHostApplicationBuilder builder)
        {
            // Register class maps
            RegisterClassMaps();

            // Register custom serializers
            RegisterCustomSerializers();

            // MongoDB client setup
            builder.Services.AddSingleton<IMongoClient, MongoClient>(_ => new MongoClient(builder.Configuration.GetConnectionString("MongoDb")!));
            builder.Services.AddSingleton(provider => provider.GetRequiredService<IMongoClient>().GetDatabase("PartyPlanning"));

            return builder;
        }

        public static void ConfigureOpenAI(this IHostApplicationBuilder builder)
        {
            builder.Services.Configure<OpenAIConfig>(options =>
            {
                IConfigurationSection? sharedConfig = SharedConfigReader.GetConfiguration()?.GetSection("OpenAI");
                builder.Configuration.Bind("OpenAI", options);

                // If there is a shared configuration, bind it to the options
                sharedConfig?.Bind(options);
            });
        }

        public static void ConfigureAgentMetadata(this IHostApplicationBuilder builder)
        {
            builder.Services.Configure<AgentConfig>(options =>
            {
                builder.Configuration.Bind("Agent", options);
            });
        }

        private static void RegisterClassMaps()
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

        private static void RegisterCustomSerializers()
        {
            BsonSerializer.RegisterSerializer(new TextContentSerializer());
            BsonSerializer.RegisterSerializer(new ImageContentSerializer());
            BsonSerializer.RegisterSerializer(new AssistantMessageContentSerializer());
            BsonSerializer.RegisterSerializer(new AuthorRoleSerializer());
        }
    }
}
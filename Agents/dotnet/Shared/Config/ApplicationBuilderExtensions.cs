using Microsoft.SemanticKernel;
using MongoDB.Bson.Serialization;
using PartyPlanning.Agents.Shared.Models;
using PartyPlanning.Agents.Shared.Serializers;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace PartyPlanning.Agents.Shared.Config
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

        public static void ConfigureWeaviate(this IHostApplicationBuilder builder)
        {
            builder.Services.Configure<WeaviateConfiguration>(options =>
            {
                IConfigurationSection? sharedConfig = SharedConfigReader.GetConfiguration()?.GetSection("Weaviate");
                builder.Configuration.Bind("Weaviate", options);

                // If there is a shared configuration, bind it to the options
                sharedConfig?.Bind(options);
            });
        }

        public static void ConfigureAgentMetadata(this IHostApplicationBuilder builder)
        {
            builder.Services.Configure<AgentConfiguration>(options =>
            {
                builder.Configuration.Bind("Agent", options);
            });
        }

        public static void ConfigurePluginServices(this IHostApplicationBuilder builder)
        {
            builder.Services.Configure<PluginServicesConfiguration>(options =>
            {
                IConfigurationSection? sharedConfig = SharedConfigReader.GetConfiguration()?.GetSection("PluginServices");
                builder.Configuration.Bind("PluginServices", options);

                // If there is a shared configuration, bind it to the options
                sharedConfig?.Bind(options);
            });
        }

        public static void ConfigureHealthCheckService(this IHostApplicationBuilder builder)
        {
            builder.Services.AddHttpClient<HealthCheckService>();
            builder.Services.AddSingleton<HealthCheckService>();
        }

        private static void RegisterClassMaps()
        {
            BsonClassMap.RegisterClassMap<KernelContent>(cm =>
            {
                cm.AutoMap();
                cm.SetIsRootClass(true);
            });

            #pragma warning disable SKEXP0001
            BsonClassMap.RegisterClassMap<FunctionCallContent>(cm =>
            {
                cm.AutoMap();
                cm.SetDiscriminator("FunctionCallContent");
                cm.MapMember(c => c.Id).SetElementName("id");
                cm.MapMember(c => c.PluginName).SetElementName("pluginName");
                cm.MapMember(c => c.FunctionName).SetElementName("functionName");
                cm.MapMember(c => c.Arguments).SetElementName("arguments");
                cm.MapMember(c => c.Exception).SetElementName("exception");
            });
            #pragma warning restore SKEXP0001

            BsonClassMap.RegisterClassMap<TextContent>(cm =>
            {
                cm.AutoMap();
                cm.SetDiscriminator("TextContent");
                cm.MapMember(c => c.Text).SetElementName("text");
                cm.MapMember(c => c.Encoding).SetElementName("encoding");
            });

            BsonClassMap.RegisterClassMap<ImageContent>(cm =>
            {
                cm.AutoMap();
                cm.SetDiscriminator("ImageContent");
            });
        }

        private static void RegisterCustomSerializers()
        {
            BsonSerializer.RegisterSerializer(new TextContentSerializer());
            BsonSerializer.RegisterSerializer(new ImageContentSerializer());
            BsonSerializer.RegisterSerializer(new FunctionCallContentSerializer());
            BsonSerializer.RegisterSerializer(new FunctionResultContentSerializer());
            BsonSerializer.RegisterSerializer(new AssistantMessageContentSerializer());
            BsonSerializer.RegisterSerializer(new AuthorRoleSerializer());
        }
    }
}
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text.Json.Serialization;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using PartyPlanning.Agents.Shared.Config;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextToImage;
using SceneService.Providers;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.Weaviate;
using Microsoft.SemanticKernel.Embeddings;

var builder = WebApplication.CreateBuilder(args);

// Register image generation service
// Add chat completion service
#pragma warning disable SKEXP0001
builder.Services.AddSingleton<ITextToImageService>((serviceProvider) => {
#pragma warning restore SKEXP0001
    var openAIConfig = serviceProvider.GetRequiredService<IOptions<OpenAIConfig>>().Value;
    
    switch(openAIConfig.DeploymentType)
    {
        #pragma warning disable SKEXP0010
        case OpenAIDeploymentType.AzureOpenAI:
            AzureOpenAITextToImageService azureOpenAITextToImageService = new (
                deploymentName: openAIConfig.DeploymentName!,
                endpoint: openAIConfig.Endpoint!,
                apiKey: openAIConfig.ApiKey,
                modelId: openAIConfig.ModelId // Optional
            );
            return azureOpenAITextToImageService;
        case OpenAIDeploymentType.OpenAI:
            OpenAITextToImageService openAITextToImageService = new (
                apiKey: openAIConfig.ApiKey,
                organization: openAIConfig.OrgId // Optional
            );
            return openAITextToImageService;
        #pragma warning restore SKEXP0010
        // Other deployment types are not supported yet
        // case OpenAIDeploymentType.Other:
        //     OpenAITextToImageService otherTextToImageService = new (
        //         apiKey: openAIConfig.ApiKey,
        //         endpoint: new Uri(openAIConfig.Endpoint!)
        //     );
        //     return otherTextToImageService;
        default:
            throw new ArgumentException("Invalid deployment type");
    }
});

#pragma warning disable SKEXP0001, SKEXP0010
builder.Services.AddSingleton<ITextEmbeddingGenerationService>((serviceProvider) => {
    OpenAIConfig openAIConfig = serviceProvider.GetRequiredService<IOptions<OpenAIConfig>>().Value;

    OpenAITextEmbeddingGenerationService openAITextEmbeddingGenerationService = new(
        modelId: "text-embedding-ada-002",
        openAIConfig.ApiKey,
        openAIConfig.OrgId // Optional
    );

    return openAITextEmbeddingGenerationService;
});
#pragma warning restore SKEXP0001, SKEXP0010

#pragma warning disable SKEXP0020, SKEXP0001
builder.Services.AddSingleton<ISemanticTextMemory>((serviceProvider) => {
    var weaviateConfig = serviceProvider.GetRequiredService<IOptions<WeaviateConfiguration>>().Value;

    MemoryBuilder memoryBuilder = new();
    WeaviateMemoryStore store = new(weaviateConfig.Endpoint!, weaviateConfig.ApiKey ?? string.Empty);
    memoryBuilder.WithMemoryStore(store);
    memoryBuilder.WithTextEmbeddingGeneration(serviceProvider.GetRequiredService<ITextEmbeddingGenerationService>());
    ISemanticTextMemory weaviateTextMemory = memoryBuilder.Build();

    if (!store.DoesCollectionExistAsync("scenes").Result)
    {
        store.CreateCollectionAsync("scenes").Wait();
    }

    return weaviateTextMemory;
});
#pragma warning restore SKEXP0020, SKEXP0001

// Register services
builder.ConfigureOpenAI();
builder.ConfigureWeaviate();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddLogging((loggingBuilder) => {loggingBuilder.AddDebug().AddConsole();});
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Light API", Version = "v1" });
    c.UseInlineDefinitionsForEnums();

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

builder.Services.AddMvc()
    .ConfigureApplicationPartManager(manager =>
    {
        manager.FeatureProviders.Add(new ControllerProvider());
    });

builder.Services.AddControllers()
    .AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Light API v1");
    c.RoutePrefix = string.Empty; // Set Swagger UI at the root
});
app.UseHealthChecks("/health");

app.MapControllers();
app.Run();

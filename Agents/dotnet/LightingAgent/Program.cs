using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text.Json.Serialization;
using Shared.Swagger;
using Shared.Converters;
using Shared.Config;
using LightingAgent.Services;
using Shared.Services;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var builder = WebApplication.CreateBuilder(args);

// Setup configuration
builder.Configuration.AddUserSecrets<Program>();
builder.ConfigureMongoDB();
builder.ConfigureOpenAI();
builder.ConfigureAgentMetadata();

// Add chat completion service
builder.Services.AddSingleton<IChatCompletionService>((serviceProvider) => {
    var openAIConfig = serviceProvider.GetRequiredService<IOptions<OpenAIConfig>>().Value;
    
    switch(openAIConfig.DeploymentType)
    {
        case OpenAIDeploymentType.AzureOpenAI:
            AzureOpenAIChatCompletionService azureOpenAIChatCompletionService = new (
                deploymentName: openAIConfig.DeploymentName!,
                apiKey: openAIConfig.ApiKey,
                endpoint: openAIConfig.Endpoint!,
                modelId: openAIConfig.ModelId // Optional
            );
            return azureOpenAIChatCompletionService;
        case OpenAIDeploymentType.OpenAI:
            OpenAIChatCompletionService openAIChatCompletionService = new (
                apiKey: openAIConfig.ApiKey,
                modelId: openAIConfig.ModelId,
                organization: openAIConfig.OrgId // Optional
            );
            return openAIChatCompletionService;
        case OpenAIDeploymentType.Other:
            #pragma warning disable SKEXP0010
            OpenAIChatCompletionService otherChatCompletionService = new (
                apiKey: openAIConfig.ApiKey,
                modelId: openAIConfig.ModelId,
                endpoint: new Uri(openAIConfig.Endpoint!)
            );
            #pragma warning restore SKEXP0010
            return otherChatCompletionService;
        default:
            throw new ArgumentException("Invalid deployment type");
    }
});

// Create kernel
builder.Services.AddSingleton((serviceProvider) => {
    Kernel kernel = new(serviceProvider);
    var openApiResourceService = serviceProvider.GetRequiredService<OpenApiResourceService>();
    var lightPluginFile = new MemoryStream(Encoding.UTF8.GetBytes(
        openApiResourceService.GetOpenApiResource(Assembly.GetExecutingAssembly(),"LightPlugin.swagger.json")));

    #pragma warning disable SKEXP0040
    kernel.ImportPluginFromOpenApiAsync(
        pluginName: "light_plugin",
        stream: lightPluginFile,
        executionParameters: new OpenApiFunctionExecutionParameters()
        {
            ServerUrlOverride = new Uri("http://localhost:5002/"),
            EnablePayloadNamespacing = true
        }
    ).Wait();
    #pragma warning restore SKEXP0040

    return kernel;
});

// Add controllers with JSON serialization
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.Converters.Add(new ListOfKernelContentConverter());
    options.JsonSerializerOptions.Converters.Add(new TextContentConverter());
    options.JsonSerializerOptions.Converters.Add(new ImageContentConverter());
    options.JsonSerializerOptions.Converters.Add(new AuthorRoleConverter());
    options.JsonSerializerOptions.Converters.Add(new AssistantMessageContentConverter());
    options.JsonSerializerOptions.Converters.Add(new AssistantThreadConverter());
    options.JsonSerializerOptions.Converters.Add(new AssistantThreadRunConverter());
});

// Add other services
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<OpenApiResourceService>();
builder.Services.AddSingleton<IRunService, LightingAgentRunService>();
builder.Services.AddTransient<AssistantEventStreamService>();

// Enable OpenAPI schema generation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Light API", Version = "v1" });
    c.UseInlineDefinitionsForEnums();
    c.EnableAnnotations();

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    // Schema filters
    c.SchemaFilter<KernelContentSchemaFilter>();
    c.SchemaFilter<AssistantMessageContentSchemaFilter>();
    c.SchemaFilter<AuthorRoleSchemaFilter>();
});

var app = builder.Build();

// Setup pages
app.MapControllers();
app.UseHealthChecks("/health");
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Light API v1");
    c.RoutePrefix = string.Empty; // Set Swagger UI at the root
});

app.Run();

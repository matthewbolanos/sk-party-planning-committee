using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text.Json.Serialization;
using PartyPlanning.Agents.Shared.Swagger;
using PartyPlanning.Agents.Shared.Converters;
using PartyPlanning.Agents.Shared.Config;
using PartyPlanning.Agents.Shared.Services;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using PartyPlanning.Agents.Shared.Controllers;
using PartyPlanning.Agents.Shared.Plugins.PythonPlanner;

 #pragma warning disable SKEXP0001

var builder = WebApplication.CreateBuilder(args);

// Setup configuration
builder.Configuration.AddUserSecrets<Program>();
builder.ConfigureMongoDB();
builder.ConfigureOpenAI();
builder.ConfigureAgentMetadata();
builder.ConfigurePluginServices();
builder.ConfigureHealthCheckService();
builder.ConfigurePythonPlanner();

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

builder.Services.AddSingleton((serviceProvider)=> {
    var PythonPlannerConfiguration = serviceProvider.GetRequiredService<IOptions<PythonPlannerConfiguration>>().Value;
    var tokenProvider = serviceProvider.GetRequiredService<AzureContainerAppTokenService>();

    var settings = new PythonPlannerExecutionSettings(
        sessionId: Guid.NewGuid().ToString(),
        endpoint: new Uri(PythonPlannerConfiguration.Endpoint));
    
    return new PythonPlanner(
        new (sessionId: Guid.NewGuid().ToString(), endpoint: new Uri(PythonPlannerConfiguration.Endpoint)),
            serviceProvider.GetRequiredService<IHttpClientFactory>(),
            tokenProvider.GetTokenAsync,
            serviceProvider.GetRequiredService<ILoggerFactory>()
    );
});

// Create native plugin collection
builder.Services.AddSingleton((serviceProvider)=>{
    var pythonInterpreter = serviceProvider.GetRequiredService<PythonPlanner>();

    KernelPluginCollection pluginCollection = [];
    pluginCollection.AddFromObject(pythonInterpreter, pluginName: "python");

    return pluginCollection;
});


builder.Services.AddSingleton<StrobeProtector>();
builder.Services.AddSingleton<IFunctionInvocationFilter,StopStrobeFilter>();

// Create kernel
builder.Services.AddTransient((serviceProvider) => {
    var pluginServicesConfig = serviceProvider.GetRequiredService<IOptions<PluginServicesConfiguration>>().Value;
    var lightPluginEndpoint = serviceProvider.GetRequiredService<HealthCheckService>().GetHealthyEndpointAsync(pluginServicesConfig["LightService"].Endpoints).Result;
    var scenePluginEndpoint = serviceProvider.GetRequiredService<HealthCheckService>().GetHealthyEndpointAsync(pluginServicesConfig["SceneService"].Endpoints).Result;
    var speakerPluginEndpoint = serviceProvider.GetRequiredService<HealthCheckService>().GetHealthyEndpointAsync(pluginServicesConfig["SpeakerService"].Endpoints).Result;
    var pluginCollection = serviceProvider.GetRequiredService<KernelPluginCollection>();

    Kernel kernel = new(serviceProvider, pluginCollection);

    var openApiResourceService = serviceProvider.GetRequiredService<OpenApiResourceService>();
    var lightPluginFile = new MemoryStream(Encoding.UTF8.GetBytes(
        openApiResourceService.GetOpenApiResource(Assembly.GetExecutingAssembly(),"LightPlugin.swagger.json")));
    var scenePluginFile = new MemoryStream(Encoding.UTF8.GetBytes(
        openApiResourceService.GetOpenApiResource(Assembly.GetExecutingAssembly(),"ScenePlugin.swagger.json")));
    var speakerPluginFile = new MemoryStream(Encoding.UTF8.GetBytes(
        openApiResourceService.GetOpenApiResource(Assembly.GetExecutingAssembly(),"SpeakerPlugin.swagger.json")));

    #pragma warning disable SKEXP0040
    // Plugin for changing lights
    kernel.ImportPluginFromOpenApiAsync(
        pluginName: "light_plugin",
        stream: lightPluginFile,
        executionParameters: new OpenApiFunctionExecutionParameters()
        {
            ServerUrlOverride = new Uri(lightPluginEndpoint),
            EnablePayloadNamespacing = true
        }
    ).Wait();
    
    // Plugin for getting scene recommendations
    kernel.ImportPluginFromOpenApiAsync(
        pluginName: "scene_plugin",
        stream: scenePluginFile,
        executionParameters: new OpenApiFunctionExecutionParameters()
        {
            ServerUrlOverride = new Uri(scenePluginEndpoint),
            EnablePayloadNamespacing = true
        }
    ).Wait();
    
    // Plugin for getting scene recommendations
    kernel.ImportPluginFromOpenApiAsync(
        pluginName: "speaker_plugin",
        stream: speakerPluginFile,
        executionParameters: new OpenApiFunctionExecutionParameters()
        {
            ServerUrlOverride = new Uri(speakerPluginEndpoint),
            EnablePayloadNamespacing = true
        }
    ).Wait();
    #pragma warning restore SKEXP0040

    return kernel;
});

// Add controllers with JSON serialization
builder.Services.AddControllers()
    .AddApplicationPart(typeof(ThreadController).Assembly)
    .AddApplicationPart(typeof(MessageController).Assembly)
    .AddApplicationPart(typeof(RunController).Assembly)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new ListOfKernelContentConverter());
        options.JsonSerializerOptions.Converters.Add(new TextContentConverter());
        options.JsonSerializerOptions.Converters.Add(new ImageContentConverter());
        options.JsonSerializerOptions.Converters.Add(new FunctionCallContentConverter());
        options.JsonSerializerOptions.Converters.Add(new FunctionResultContentConverter());
        options.JsonSerializerOptions.Converters.Add(new AuthorRoleConverter());
        options.JsonSerializerOptions.Converters.Add(new AssistantMessageContentConverter());
        options.JsonSerializerOptions.Converters.Add(new AssistantThreadConverter());
        options.JsonSerializerOptions.Converters.Add(new AssistantThreadRunConverter());
    });

// Add other services
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<OpenApiResourceService>();
builder.Services.AddSingleton<RunService>();
builder.Services.AddTransient<AssistantEventStreamService>();
builder.Services.AddLogging((loggingBuilder) => {loggingBuilder.AddDebug().AddConsole().SetMinimumLevel(LogLevel.Trace);});

// Enable OpenAPI schema generation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Lighting Agent API", Version = "v1" });
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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lighting Agent API v1");
    c.RoutePrefix = string.Empty; // Set Swagger UI at the root
});

app.Run();


public class StopStrobeFilter : IFunctionInvocationFilter
{
    private readonly StrobeProtector _strobeProtector;

    public StopStrobeFilter(StrobeProtector strobeProtector)
    {
        _strobeProtector = strobeProtector;
    }

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        var fullName = context.Function.PluginName + "." + context.Function.Name;
        
        switch (fullName)
        {
            case "light_plugin.change_light_state":
                if (!_strobeProtector.TestForStrobe(context))
                {
                    await next(context);
                }
                break;
            default:
                await next(context);
                break;
        }
    }
}
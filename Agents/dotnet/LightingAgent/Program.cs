using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text.Json.Serialization;
using MongoDB.Driver;
using Shared.Swagger;
using Shared.Converters;
using SharedConfig.Models;
using SharedConfig;
using LightingAgent.Services;
using Shared.Utilities;

var builder = WebApplication.CreateBuilder(args);

// Add MongoDB
MongoDBUtility.ConfigureMongoDB(builder.Services, builder.Configuration.GetConnectionString("MongoDb")!);

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

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.Converters.Add(new ListOfKernelContentConverter());
    options.JsonSerializerOptions.Converters.Add(new TextContentConverter());
    options.JsonSerializerOptions.Converters.Add(new ImageContentConverter());
    options.JsonSerializerOptions.Converters.Add(new AuthorRoleConverter());
    options.JsonSerializerOptions.Converters.Add(new AssistantMessageContentConverter());
    options.JsonSerializerOptions.Converters.Add(new AssistantThreadConverter());
});

// Setup configuration
builder.Configuration.AddUserSecrets<Program>();
builder.Services.Configure<OpenAIConfig>(options =>
{
    IConfigurationSection? sharedConfig = SharedConfigReader.GetConfiguration()?.GetSection("OpenAI");
    builder.Configuration.Bind("OpenAI", options);

    // If there is a shared configuration, bind it to the options
    if (sharedConfig != null)
    {
        sharedConfig.Bind(options);
    }
});
builder.Services.Configure<AgentConfig>(options =>
{
    builder.Configuration.Bind("Agent", options);
});

// Enable loading of OpenAPI schema files
builder.Services.AddSingleton<OpenApiResourceService>();

// Add the service that uses Semantic Kernel to run the chat completion
builder.Services.AddSingleton<IRunService, LightingAgentRunService>();

// Add the utility to handle AssistantEventStream
builder.Services.AddTransient<AssistantEventStreamUtility>();

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

app.MapControllers();
app.Run();

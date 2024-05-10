using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using SharedConfig;
using SharedConfig.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Home API", Version = "v1" });
    c.UseInlineDefinitionsForEnums();
    c.EnableAnnotations();

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// Configure JsonStringEnumConverter
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Add secrets using the User Secrets ID
builder.Configuration.AddUserSecrets<Program>();

// Load shared settings and User Secrets
builder.Services.Configure<OpenAIConfig>(options =>
{
    var sharedConfig = SharedConfigReader.GetConfiguration().GetSection("OpenAI");
    builder.Configuration.Bind("OpenAI", options);
    sharedConfig.Bind(options);
});

// Provide service to load in OpenAPI resources
builder.Services.AddSingleton<OpenApiResourceService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Home API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the root
    });
}

app.UseHttpsRedirection();

app.Run();
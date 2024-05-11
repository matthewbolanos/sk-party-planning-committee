using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text.Json.Serialization;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddSingleton<IMongoClient, MongoClient>(_ => new MongoClient(builder.Configuration.GetConnectionString("MongoDb")));
builder.Services.AddSingleton(provider => provider.GetRequiredService<IMongoClient>().GetDatabase("HomeAutomation"));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Light API", Version = "v1" });
    c.UseInlineDefinitionsForEnums();

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

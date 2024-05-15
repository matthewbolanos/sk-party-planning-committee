using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.SemanticKernel;
using PartyPlanning.Agents.Shared.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PartyPlanning.Agents.Shared.Swagger
{
    public class AssistantThreadSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(List<KernelContent>))
            {
                schema.Example = new OpenApiObject
                {
                    
                    ["id"] = new OpenApiString("663ef31ed04a068ed8ed1fef"),
                    ["object"] = new OpenApiString("thread"),
                    ["created_at"] = new OpenApiLong(5250196001451743000)
                };
            }
        }
    }
}
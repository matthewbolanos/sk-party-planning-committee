using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.SemanticKernel;
using Shared.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Shared.Swagger
{
    public class KernelContentSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(KernelContent))
            {
                schema.Example = new OpenApiObject
                {
                    
                    ["type"] = new OpenApiString("text"),
                    ["text"] = new OpenApiString("How does AI work? Explain it in simple terms.")
                };
            }
        }
    }
}

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Shared.Swagger
{
    public class AuthorRoleSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(AuthorRole))
            {
                schema.Example = new OpenApiString("user");
            }
        }
    }
}

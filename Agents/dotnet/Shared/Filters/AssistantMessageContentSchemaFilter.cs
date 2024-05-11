using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Shared.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Shared.Swagger
{
    public class AssistantMessageContentSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            // Check if the schema type is AssistantMessageContent
            if (context.Type == typeof(AssistantMessageContent))
            {

                var KernelContentSchema = context.SchemaGenerator.GenerateSchema(typeof(KernelContent), context.SchemaRepository);
                var KernelContentExample = KernelContentSchema.Example;

                var authorRoleSchema = context.SchemaGenerator.GenerateSchema(typeof(AuthorRole), context.SchemaRepository);
                var authorRoleExample = authorRoleSchema.Example;

                schema.Example = new OpenApiObject
                {
                    ["id"] = new OpenApiString("msg_abc123"),
                    ["object"] = new OpenApiString("thread.message"),
                    ["created_at"] = new OpenApiLong(1699017614),
                    ["assistant_id"] = new OpenApiNull(),
                    ["thread_id"] = new OpenApiString("thread_abc123"),
                    ["run_id"] = new OpenApiNull(),
                    ["role"] = authorRoleExample,
                    ["content"] = KernelContentExample,
                };
            }
        }
    }
}

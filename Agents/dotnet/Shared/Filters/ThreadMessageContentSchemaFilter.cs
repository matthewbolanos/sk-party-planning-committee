using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Shared.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Shared.Swagger
{
    public class ThreadMessageContentSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            // Check if the schema type is ThreadMessageContent
            if (context.Type == typeof(ThreadMessageContent))
            {
                schema.Example = new OpenApiObject
                {
                    ["id"] = new OpenApiString("msg_abc123"),
                    ["object"] = new OpenApiString("thread.message"),
                    ["created_at"] = new OpenApiLong(1699017614),
                    ["assistant_id"] = new OpenApiNull(),
                    ["thread_id"] = new OpenApiString("thread_abc123"),
                    ["run_id"] = new OpenApiNull(),
                    ["role"] = new OpenApiString("user"),
                    ["content"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["type"] = new OpenApiString("text"),
                            ["text"] = new OpenApiObject
                            {
                                ["value"] = new OpenApiString("How does AI work? Explain it in simple terms."),
                            }
                        }
                    },
                };
            }
        }
    }
}

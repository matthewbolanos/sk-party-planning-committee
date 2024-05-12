using Microsoft.SemanticKernel;
using Shared.Models;
using SharedConfig.Models;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using System.Reflection;
using MongoDB.Driver;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text;
using Shared.Utilities;
using Microsoft.Extensions.Options;

namespace LightingAgent.Services
{
    /// <summary>
    /// Service for running a chat completion.
    /// </summary>
    /// <param name="database"></param>
    /// <param name="openAIConfig"></param>
    /// <param name="openApiResourceService"></param>
    /// <param name="assistantEventStreamUtility"></param>
    public class LightingAgentRunService(
        IMongoDatabase database,
        IOptions<OpenAIConfig> openAIConfig,
        OpenApiResourceService openApiResourceService,
        AssistantEventStreamUtility assistantEventStreamUtility
    ) : IRunService
    {
        private readonly IMongoCollection<AssistantMessageContent> _messagesCollection = database.GetCollection<AssistantMessageContent>("Messages");
        private readonly OpenAIConfig _openAIConfig = openAIConfig.Value;
        /// <summary>
        /// Executes a run.
        /// </summary>
        /// <param name="run"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async IAsyncEnumerable<string> ExecuteRunAsync(AssistantThreadRun run)
        {
            // Create kernel
            IKernelBuilder kernelBuilder = Kernel.CreateBuilder();

            // Add AI services
            switch (_openAIConfig.DeploymentType)
            {
                case OpenAIDeploymentType.AzureOpenAI:
                    kernelBuilder.AddAzureOpenAIChatCompletion(
                        deploymentName: _openAIConfig.DeploymentName!,
                        apiKey: _openAIConfig.ApiKey,
                        endpoint: new(_openAIConfig.Endpoint!),
                        modelId: _openAIConfig.ModelId // Optional
                    );
                    break;
                case OpenAIDeploymentType.OpenAI:
                    kernelBuilder.AddOpenAIChatCompletion(
                        apiKey: _openAIConfig.ApiKey,
                        modelId: _openAIConfig.ModelId,
                        orgId: _openAIConfig.OrgId // Optional
                    );
                    break;
                case OpenAIDeploymentType.Other:
                    // With the endpoint property, you can target any OpenAI-compatible API
                    #pragma warning disable SKEXP0010
                    kernelBuilder.AddOpenAIChatCompletion(
                        apiKey: _openAIConfig.ApiKey,
                        modelId: _openAIConfig.ModelId,
                        endpoint: new(_openAIConfig.Endpoint!)
                    );
                    #pragma warning restore SKEXP0010
                    break;
                default:
                    throw new ArgumentException("Invalid deployment type");
            }

            // Load filters

            // Build the kernel
            Kernel kernel = kernelBuilder.Build();

            // Load the OpenAPI plugins
            #pragma warning disable SKEXP0040
            await kernel.ImportPluginFromOpenApiAsync(
                pluginName: "LightPlugin",
                stream: new MemoryStream(Encoding.UTF8.GetBytes(openApiResourceService.GetOpenApiResource(
                    Assembly.GetExecutingAssembly(),
                    "LightPlugin.swagger.json"))
                )
            );
            #pragma warning restore SKEXP0040

            // Load all the messages (chat history) from MongoDB using the thread ID and sort them by creation date
            var messages = await _messagesCollection.Find(m => m.ThreadId == run.ThreadId).SortBy(m => m.CreatedAt).ToListAsync();
            ChatHistory chatHistory = new(messages);

            // Invoke the chat completion service
            IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            var results = chatCompletionService.GetStreamingChatMessageContentsAsync(
                chatHistory: chatHistory,
                executionSettings: new OpenAIPromptExecutionSettings() {
                    // Allows the AI to automatically choose and invoke functions from the kernel's plugins
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                },
                kernel: kernel
            );

            // Return the results as a stream
            var completeMessage = new StringBuilder();
            await foreach (var result in results)
            {
                completeMessage.Append(result.ToString());

                // Send the message events to the client
                var events = assistantEventStreamUtility.CreateMessageEvent(run.Id, result);
                foreach (var messageEvent in events)
                {
                    yield return messageEvent;
                }
            }

            // Save the results to MongoDB
            await _messagesCollection.InsertOneAsync(
                new AssistantMessageContent() {
                    AssistantId = run.AssistantId,
                    Role = AuthorRole.Assistant,
                    ThreadId = run.ThreadId,
                    Content = completeMessage.ToString()
                }
            );
        }
    }
}
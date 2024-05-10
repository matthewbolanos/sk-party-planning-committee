using Microsoft.SemanticKernel;
using Shared.Models;
using SharedConfig.Models;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using System.Reflection;
using MongoDB.Driver;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text;

namespace LightingAgent.Services
{
    /// <summary>
    /// Service for running a chat completion.
    /// </summary>
    /// <param name="database"></param>
    /// <param name="openAIConfig"></param>
    /// <param name="openApiResourceService"></param>
    public class RunService(
        IMongoDatabase database,
        OpenAIConfig openAIConfig,
        OpenApiResourceService openApiResourceService
    ) : IRunService
    {
        private readonly IMongoCollection<AssistantThread> _threadsCollection = database.GetCollection<AssistantThread>("threads");
        private readonly IMongoCollection<ThreadMessageContent> _messagesCollection = database.GetCollection<ThreadMessageContent>("threads");

        /// <summary>
        /// Executes a run.
        /// </summary>
        /// <param name="run"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async IAsyncEnumerable<string> ExecuteRunAsync(Run run)
        {
            // Create kernel
            IKernelBuilder kernelBuilder = Kernel.CreateBuilder();

            // Add AI services
            switch (openAIConfig.DeploymentType)
            {
                case OpenAIDeploymentType.AzureOpenAI:
                    kernelBuilder.AddAzureOpenAIChatCompletion(
                        deploymentName: openAIConfig.DeploymentName!,
                        apiKey: openAIConfig.ApiKey,
                        endpoint: new(openAIConfig.Endpoint!),
                        modelId: openAIConfig.ModelId // Optional
                    );
                    break;
                case OpenAIDeploymentType.OpenAI:
                    kernelBuilder.AddOpenAIChatCompletion(
                        apiKey: openAIConfig.ApiKey,
                        modelId: openAIConfig.ModelId,
                        orgId: openAIConfig.OrgId // Optional
                    );
                    break;
                case OpenAIDeploymentType.Other:
                    // With the endpoint property, you can target any OpenAI-compatible API
                    #pragma warning disable SKEXP0010
                    kernelBuilder.AddOpenAIChatCompletion(
                        apiKey: openAIConfig.ApiKey,
                        modelId: openAIConfig.ModelId,
                        endpoint: new(openAIConfig.Endpoint!)
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
                stream: new MemoryStream(Encoding.UTF8.GetBytes(openApiResourceService.GetOpenApiResource("LightPlugin.swagger.json")))
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
                completeMessage.AppendLine(result.ToString());
                yield return result.ToString();
            }

            // Save the results to MongoDB
            await _messagesCollection.InsertOneAsync(
                new ThreadMessageContent() {
                    ThreadId = run.ThreadId,
                    Content = completeMessage.ToString()
                }
            );
        }

    }
}
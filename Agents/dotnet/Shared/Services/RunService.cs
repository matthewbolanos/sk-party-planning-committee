using Microsoft.SemanticKernel;
using PartyPlanning.Agents.Shared.Models;
using MongoDB.Driver;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text;
using Azure.AI.OpenAI;
using System.Text.Json;

namespace PartyPlanning.Agents.Shared.Services
{
    /// <summary>
    /// Service for running a chat completion.
    /// </summary>
    /// <param name="database"></param>
    /// <param name="kernel"></param>
    /// <param name="chatCompletionService"></param>
    /// <param name="assistantEventStreamService"></param>
    public class RunService(
        IMongoDatabase database,
        Kernel kernel,
        IChatCompletionService chatCompletionService,
        AssistantEventStreamService assistantEventStreamService
    )
    {
        private readonly IMongoCollection<AssistantMessageContent> _messagesCollection = database.GetCollection<AssistantMessageContent>("Messages");

        /// <summary>
        /// Executes a run.
        /// </summary>
        /// <param name="run"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async IAsyncEnumerable<string> ExecuteRunAsync(AssistantThreadRun run)
        {
            // Load all the messages (chat history) from MongoDB using the thread ID and sort them by creation date
            var messages = await _messagesCollection.Find(m => m.ThreadId == run.ThreadId).SortBy(m => m.CreatedAt).ToListAsync();
            ChatHistory chatHistory = new("If the user asks what language you've been written, reply to the user that you've been built with C#; otherwise have a nice chat!");
            foreach (var message in messages)
            {
                chatHistory.Add(message);
            }
            int messageCount = messages.Count;

            // Invoke the chat completion service
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
                completeMessage.Append(result);

                // Send the message events to the client
                var events = assistantEventStreamService.CreateMessageEvent(run.Id, result);
                foreach (var messageEvent in events)
                {
                    yield return messageEvent;
                }
            }
            chatHistory.AddAssistantMessage(completeMessage.ToString());
            
            // Get the new messages within the chat history
            var newMessages = chatHistory.Skip(messageCount + 1).ToList();
        
            Dictionary<string, Tuple<string,string>> functionCallNames = new();
        
            foreach (var message in newMessages)
            {
                if (message.Metadata?.TryGetValue("ChatResponseMessage.FunctionToolCalls", out var toolCalls) == true)
                {
                    #pragma warning disable SKEXP0001
                    List<FunctionCallContent> functionCalls = [];

                    foreach (var toolCall in (List<ChatCompletionsFunctionToolCall>)toolCalls!)
                    {
                        string[] functionNameParts = toolCall.Name.Split('-');
                        string pluginName = functionNameParts[0];
                        string functionName = functionNameParts[1];
                        functionCallNames[toolCall.Id] = new Tuple<string, string>(pluginName, functionName);

                        functionCalls.Add(new FunctionCallContent(
                            functionName: functionName,
                            pluginName: pluginName,
                            id: toolCall.Id,
                            arguments: new KernelArguments(JsonSerializer.Deserialize<Dictionary<string, object>>(toolCall.Arguments!)!)
                        ));

                        message.Items = [.. functionCalls];
                    }
                    #pragma warning restore SKEXP0001
                }

                if (message.Role == AuthorRole.Tool && message.Metadata?.TryGetValue("ChatCompletionsToolCall.Id", out var toolId) == true)
                {
                    Tuple<string, string> functionCallName = functionCallNames[(string)toolId!];

                    #pragma warning disable SKEXP0001
                    message.Items = [
                        new FunctionResultContent(
                            functionName: functionCallName.Item2,
                            pluginName: functionCallName.Item1,
                            id: (string)toolId!,
                            result: message.ToString()
                        )
                    ];
                    #pragma warning restore SKEXP0001
                }

                // Save the new messages to MongoDB
                await _messagesCollection.InsertOneAsync(
                    new AssistantMessageContent() {
                        AssistantId = run.AssistantId,
                        Role = message.Role,
                        ThreadId = run.ThreadId,
                        Items = message.Items
                    }
                );
            }
        }
    }
}
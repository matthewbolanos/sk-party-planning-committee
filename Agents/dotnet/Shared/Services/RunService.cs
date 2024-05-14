using Microsoft.SemanticKernel;
using Shared.Models;
using MongoDB.Driver;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text;

namespace Shared.Services
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
        
            foreach (var message in newMessages)
            {
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

using System.Text;
using System.Text.Json;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Shared.Config;
using Shared.Models;

namespace Shared.Utilities
{
    public class AssistantEventStreamUtility(
        IOptions<AgentConfig> agentConfiguration,
        IOptions<Microsoft.AspNetCore.Mvc.JsonOptions> jsonOptions
    )
    {
        private string? _currentThreadId = null;
        private AssistantMessageContent? _currentMessage = null;
        private AgentConfig _agentConfiguration = agentConfiguration.Value;
        private StringBuilder _messageBuilder = new();

        public IEnumerable<string> CreateMessageEvent(string runId, StreamingChatMessageContent data)
        {
            // Check if the type is not OpenAIStreamingChatMessageContent (if so, fail)
            if (data.GetType() != typeof(OpenAIStreamingChatMessageContent))
            {
                yield return CreateErrorEvent("Only OpenAI chat completion APIs are supported.");
                yield break;
            }

            // Get the inner content
            var streamingChatCompletionsUpdate = (StreamingChatCompletionsUpdate)data.InnerContent!;

            // Check if the message was not closed properly
            if (_currentMessage != null && streamingChatCompletionsUpdate.Id != _currentMessage.Id)
            {
                yield return CreateErrorEvent("Previous message was not finished.");
                yield break;
            }

            // Check if the message is new
            if (_currentMessage == null)
            {
                // Create new message object
                _currentMessage = new AssistantMessageContent()
                {
                    Id = streamingChatCompletionsUpdate.Id,
                    ThreadId = _currentThreadId,
                    CreatedAt = DateTime.Now,
                    Role = AuthorRole.Assistant,
                    AssistantId = _agentConfiguration.Name,
                    RunId = runId,
                    Items = []
                };

                // Create new message events
                yield return CreateEvent("thread.message.created", _currentMessage);
                yield return CreateEvent("thread.message.in_progress", _currentMessage);
            }

            // Add the message delta to the complete message
            _messageBuilder.Append(data.Content);

            // Create message delta event
            yield return CreateEvent("thread.message.delta", data);

            // Check if the message is done
            if (streamingChatCompletionsUpdate.FinishReason != null)
            {
                // Update the message content
                _currentMessage.Items = [new TextContent(_messageBuilder.ToString())];

                // Create done message events
                yield return CreateEvent("thread.message.completed", _currentMessage);
                _currentMessage = null;
            }
        }

        public string CreateEvent<T>(string eventType, T data)
        {
            string jsonData = JsonSerializer.Serialize(data, options: jsonOptions.Value.JsonSerializerOptions);
            return $"event: {eventType}\n" +
                $"data: {jsonData}\n\n";
        }

        public string CreateErrorEvent(string message)
        {
            var errorData = new { message = message };
            string jsonData = JsonSerializer.Serialize(errorData, options: jsonOptions.Value.JsonSerializerOptions);
            return $"event: error\n" +
                $"data: {jsonData}\n\n";
        }

        public string CreateDoneEvent()
        {
            return "event: done\n" +
                "data: [DONE]\n\n";
        }
    }

}
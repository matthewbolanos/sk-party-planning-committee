import com.fasterxml.jackson.databind.ObjectMapper;
import com.microsoft.semantickernel.services.chatcompletion.AuthorRole;
import com.microsoft.semantickernel.services.chatcompletion.ChatMessageContent;
import com.microsoft.semantickernel.services.textcompletion.TextContent;
import com.partyplanning.lightingagent.models.AssistantMessageContent;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Component;
import org.springframework.web.servlet.mvc.method.annotation.SseEmitter;
import com.microsoft.semantickernel.aiservices.openai.chatcompletion.OpenAIChatMessageContent;
import java.io.IOException;
import java.util.Date;

@Component
public class AssistantEventStreamUtility {


    private ObjectMapper objectMapper = new ObjectMapper();
    private AssistantMessageContent currentMessage;
    private StringBuilder messageBuilder = new StringBuilder();

    public void sendEvent(SseEmitter emitter, String eventType, Object data) {
        try {
            String jsonData = objectMapper.writeValueAsString(data);
            emitter.send(SseEmitter.event().name(eventType).data(jsonData));
        } catch (IOException e) {
            throw new RuntimeException("Failed to send SSE", e);
        }
    }

    public Iterable<String> createMessageEvent(String runId, ChatMessageContent data) {
        if (!(data instanceof OpenAIChatMessageContent)) {
            yield return createErrorEvent("Only OpenAI chat completion APIs are supported.");
            return;
        }

        OpenAIStreamingChatMessageContent streamingData = (OpenAIStreamingChatMessageContent) data;

        if (currentMessage != null && !streamingData.getId().equals(currentMessage.getId())) {
            yield return createErrorEvent("Previous message was not finished.");
            return;
        }

        if (currentMessage == null) {
            currentMessage = new AssistantMessageContent();
            currentMessage.setId(streamingData.getId());
            currentMessage.setThreadId(currentThreadId); // Assuming there's a way to track the current thread ID
            currentMessage.setCreatedAt(new Date());
            currentMessage.setRole(AuthorRole.ASSISTANT);
            currentMessage.setAssistantId(agentConfig.getName());
            currentMessage.setRunId(runId);

            yield return createEvent("thread.message.created", currentMessage);
            yield return createEvent("thread.message.in_progress", currentMessage);
        }

        messageBuilder.append(data.getContent());
        yield return createEvent("thread.message.delta", data);

        if (streamingData.getFinishReason() != null) {
            currentMessage.setItems(List.of(new TextContent(messageBuilder.toString())));
            yield return createEvent("thread.message.completed", currentMessage);
            currentMessage = null;
        }
    }

    public String createEvent(String eventType, Object data) {
        try {
            String jsonData = objectMapper.writeValueAsString(data);
            return "event: " + eventType + "\ndata: " + jsonData + "\n\n";
        } catch (Exception e) {
            throw new RuntimeException("Failed to serialize event data", e);
        }
    }

    public String createErrorEvent(String message) {
        try {
            String jsonData = objectMapper.writeValueAsString(new ErrorData(message));
            return "event: error\ndata: " + jsonData + "\n\n";
        } catch (Exception e) {
            throw new RuntimeException("Failed to serialize error data", e);
        }
    }

    public String createDoneEvent() {
        return "event: done\ndata: [DONE]\n\n";
    }

    private static class ErrorData {
        private String message;

        public ErrorData(String message) {
            this.message = message;
        }

        // Getter and Setter
    }
}

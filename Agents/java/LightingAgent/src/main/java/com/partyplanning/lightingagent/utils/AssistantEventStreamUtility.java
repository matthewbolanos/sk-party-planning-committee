package com.partyplanning.lightingagent.utils;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.microsoft.semantickernel.services.chatcompletion.ChatMessageContent;
import com.partyplanning.lightingagent.models.AssistantMessageContent;
import com.partyplanning.lightingagent.models.AssistantThreadRun;

import org.springframework.stereotype.Component;
import org.springframework.web.servlet.mvc.method.annotation.SseEmitter;
import com.microsoft.semantickernel.aiservices.openai.chatcompletion.OpenAIChatMessageContent;
import java.io.IOException;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;

@Component
public class AssistantEventStreamUtility {
    private ObjectMapper objectMapper = new ObjectMapper();

    public void sendEvent(SseEmitter emitter, String eventType, Object data) {
        try {
            String jsonData = objectMapper.writeValueAsString(data);
            emitter.send(SseEmitter.event().name(eventType).data(jsonData));
        } catch (IOException e) {
            throw new RuntimeException("Failed to send SSE", e);
        }
    }

    @SuppressWarnings({ "rawtypes", "unchecked" })
    public void sendMessageEvent(SseEmitter emitter, AssistantThreadRun run, ChatMessageContent data) {
        List<String> events = new ArrayList<>();

        if (!(data instanceof OpenAIChatMessageContent)) {
            sendEvent(emitter, "error", new ErrorData("Only OpenAI chat completion APIs are supported."));
        }

        OpenAIChatMessageContent openaiData = (OpenAIChatMessageContent) data;

        AssistantMessageContent currentMessage = new AssistantMessageContent(
            run.getThreadId(),
            openaiData.getAuthorRole(),
            data.getItems(),
            run.getId(),
            run.getAssistantId(),
            new Date()
        );

        sendEvent(emitter, "thread.message.created", currentMessage);
        sendEvent(emitter, "thread.message.in_progress", currentMessage);
        sendEvent(emitter, "thread.message.delta", data);
        sendEvent(emitter, "thread.message.completed", currentMessage);
    }

    public void sendErrorEvent(SseEmitter emitter, String message) {
        sendEvent(emitter, "error", new ErrorData(message));
    }

    public void sendDoneEvent(SseEmitter emitter) {
        sendEvent(emitter, "done", "[DONE]");
    }

    private static class ErrorData {
        private String message;

        public ErrorData(String message) {
            this.message = message;
        }

        // Getter and Setter
    }
}

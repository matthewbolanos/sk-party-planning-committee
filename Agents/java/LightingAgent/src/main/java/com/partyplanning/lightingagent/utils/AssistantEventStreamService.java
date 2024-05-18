package com.partyplanning.lightingagent.utils;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.microsoft.semantickernel.services.chatcompletion.AuthorRole;
import com.microsoft.semantickernel.services.chatcompletion.ChatMessageContent;
import com.microsoft.semantickernel.services.textcompletion.TextContent;
import com.partyplanning.lightingagent.models.AssistantMessageContent;
import com.partyplanning.lightingagent.models.AssistantThreadRun;
import com.partyplanning.lightingagent.models.MessageDelta;

import org.apache.commons.lang3.StringUtils;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Component;
import org.springframework.web.servlet.mvc.method.annotation.SseEmitter;
import com.microsoft.semantickernel.aiservices.openai.chatcompletion.OpenAIChatMessageContent;
import java.io.IOException;
import java.util.ArrayList;
import java.util.Date;

@Component
public class AssistantEventStreamService {
    @Autowired
    private ObjectMapper objectMapper;

    public void sendEvent(SseEmitter emitter, String eventType, Object data) {
        try {
            // Use the autowired ObjectMapper to serialize data
            String jsonData = objectMapper.writeValueAsString(data);
            emitter.send(SseEmitter.event().name(eventType).data(jsonData));
        } catch (IOException e) {
            throw new RuntimeException("Failed to send SSE", e);
        }
    }

    @SuppressWarnings({ "rawtypes", "unchecked" })
    public void sendMessageEvent(SseEmitter emitter, AssistantThreadRun run, ChatMessageContent data) {
        if (!(data instanceof OpenAIChatMessageContent)) {
            sendEvent(emitter, "error", new ErrorData("Only OpenAI chat completion APIs are supported."));
        }

        OpenAIChatMessageContent openaiData = (OpenAIChatMessageContent) data;

        if (openaiData.getToolCall() == null && openaiData.getAuthorRole() != AuthorRole.TOOL && !StringUtils.isEmpty(openaiData.getContent()))
        {
            var items = new ArrayList();
            items.add(new TextContent(data.getContent(), null, null));

            AssistantMessageContent currentMessage = new AssistantMessageContent(
                run.getThreadId(),
                openaiData.getAuthorRole(),
                items,
                run.getId(),
                run.getAssistantId(),
                new Date()
            );

            MessageDelta delta = new MessageDelta();
            delta.setId(run.getId());
            delta.setContent(items);

            sendEvent(emitter, "thread.message.created", currentMessage);
            sendEvent(emitter, "thread.message.in_progress", currentMessage);
            sendEvent(emitter, "thread.message.delta", delta);
            sendEvent(emitter, "thread.message.completed", currentMessage);
        }
        
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

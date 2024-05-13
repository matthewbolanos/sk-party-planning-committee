package com.partyplanning.lightingagent.models;

import org.springframework.data.mongodb.core.mapping.Document;
import java.util.List;

@Document(collection = "Threads")
public class AssistantThread extends AssistantThreadBase {

    @SuppressWarnings("rawtypes")
    private List<AssistantMessageContent> messages;

    // Getters and Setters
    @SuppressWarnings("rawtypes")
    public List<AssistantMessageContent> getMessages() {
        return messages;
    }

    public void setMessages(@SuppressWarnings("rawtypes") List<AssistantMessageContent> messages) {
        this.messages = messages;
    }
}

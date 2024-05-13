package com.partyplanning.lightingagent.models;

import org.springframework.data.mongodb.core.mapping.Document;
import java.util.List;

@Document(collection = "Threads")
@SuppressWarnings("rawtypes")
public class AssistantThread extends AssistantThreadBase {

    private List<AssistantMessageContent> messages;

    // Getters and Setters
    public List<AssistantMessageContent> getMessages() {
        return messages;
    }

    public void setMessages(List<AssistantMessageContent> messages) {
        this.messages = messages;
    }
}

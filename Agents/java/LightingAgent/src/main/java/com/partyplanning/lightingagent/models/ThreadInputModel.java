package com.partyplanning.lightingagent.models;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.util.List;

public class ThreadInputModel {
    @JsonProperty("messages")
    private List<AssistantMessageContentInputModel> messages;

    // Constructors
    public ThreadInputModel() {
        // Default constructor
    }

    // Getters and Setters
    public List<AssistantMessageContentInputModel> getMessages() {
        return messages;
    }

    public void setMessages(List<AssistantMessageContentInputModel> messages) {
        this.messages = messages;
    }
}

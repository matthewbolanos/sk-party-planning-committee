package com.partyplanning.lightingagent.models;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.util.List;

public class ThreadInputModel {

    @SuppressWarnings("rawtypes")
    @JsonProperty("messages")
    private List<AssistantMessageContentInputModel> messages;

    // Constructors
    public ThreadInputModel() {
        // Default constructor
    }

    // Getters and Setters
    @SuppressWarnings("rawtypes")
    public List<AssistantMessageContentInputModel> getMessages() {
        return messages;
    }

    public void setMessages(@SuppressWarnings("rawtypes") List<AssistantMessageContentInputModel> messages) {
        this.messages = messages;
    }
}

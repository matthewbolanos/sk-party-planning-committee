package com.partyplanning.lightingagent.models;

import java.util.List;

import com.microsoft.semantickernel.services.KernelContent;

@SuppressWarnings("rawtypes")
public class MessageDelta {
    private String id;
    private String object = "thread.message.delta";
    private List<KernelContent> content;

    // Getters and setters
    public String getId() {
        return id;
    }

    public void setId(String id) {
        this.id = id;
    }

    public String getObject() {
        return object;
    }

    public void setObject(String object) {
        this.object = object;
    }

    public List<KernelContent> getContent() {
        return content;
    }

    public void setContent(List<KernelContent> delta) {
        this.content = delta;
    }
}
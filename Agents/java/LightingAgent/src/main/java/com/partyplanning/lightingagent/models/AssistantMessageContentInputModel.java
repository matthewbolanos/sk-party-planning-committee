package com.partyplanning.lightingagent.models;

import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.databind.annotation.JsonDeserialize;
import com.microsoft.semantickernel.services.chatcompletion.AuthorRole;
import com.partyplanning.lightingagent.converters.AuthorRoleDeserializer;
import com.microsoft.semantickernel.services.KernelContent;
import java.util.List;

public class AssistantMessageContentInputModel {

    @JsonProperty("role")
    @JsonDeserialize(using = AuthorRoleDeserializer.class)
    private AuthorRole role;

    @JsonProperty("content")
    private List<KernelContent<?>> content;

    // Constructors
    public AssistantMessageContentInputModel() {
    }

    // Getters and Setters
    public AuthorRole getRole() {
        return role;
    }

    public void setRole(AuthorRole role) {
        this.role = role;
    }

    public List<KernelContent<?>> getContent() {
        return content;
    }

    public void setContent(List<KernelContent<?>> content) {
        this.content = content;
    }
}

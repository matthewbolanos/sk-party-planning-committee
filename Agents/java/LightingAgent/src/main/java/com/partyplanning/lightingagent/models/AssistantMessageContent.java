package com.partyplanning.lightingagent.models;

import org.bson.types.ObjectId;
import org.springframework.data.annotation.Id;
import org.springframework.data.annotation.PersistenceCreator;
import org.springframework.data.mongodb.core.mapping.Document;
import org.springframework.data.mongodb.core.mapping.Field;
import org.springframework.data.repository.query.Param;

import com.fasterxml.jackson.annotation.JsonProperty;
import com.microsoft.semantickernel.orchestration.FunctionResultMetadata;
import com.microsoft.semantickernel.services.KernelContent;
import com.microsoft.semantickernel.services.chatcompletion.AuthorRole;
import com.microsoft.semantickernel.services.chatcompletion.ChatMessageContent;

import javax.annotation.Nullable;

import java.util.Collections;
import java.util.Date;
import java.util.List;

@Document(collection = "Messages")
public class AssistantMessageContent {


    @PersistenceCreator
    public AssistantMessageContent(
        @Param("threadId") String threadId,
        @Param("authorRole") AuthorRole authorRole,
        @Param("items") List<KernelContent<?>> items,
        @Param("runId") @Nullable String runId,
        @Param("assistantId") @Nullable String assistantId,
        @Param("createdAt") @Nullable Date createdAt
    ) {
        // super(authorRole, items, modelId, innerContent, encoding, metadata);
        this.threadId = threadId;
        this.authorRole = authorRole;
        this.items = items;
        this.runId = runId;
        this.assistantId = assistantId;
        this.createdAt = createdAt == null ? new Date() : createdAt;
    }

    @Id
    private String id = ObjectId.get().toString();

    @Field("thread_id")
    @JsonProperty("thread_id")
    private String threadId;

    @Field("run_id")
    @JsonProperty("run_id")
    private String runId;

    @Field("assistant_id")
    @JsonProperty("assistant_id")
    private String assistantId;

    @Field("created_at")
    @JsonProperty("created_at")
    private Date createdAt = new Date();

    @Field("role")
    @JsonProperty("role")
    private AuthorRole authorRole;

    @Field("content")
    @JsonProperty("content")
    private List<KernelContent<?>> items;

    public String getId() {
        return id;
    }

    public void setId(String id) {
        this.id = id;
    }

    public String getThreadId() {
        return threadId;
    }

    public void setThreadId(String threadId) {
        this.threadId = threadId;
    }

    public String getRunId() {
        return runId;
    }

    public void setRunId(String runId) {
        this.runId = runId;
    }

    public String getAssistantId() {
        return assistantId;
    }

    public void setAssistantId(String assistantId) {
        this.assistantId = assistantId;
    }

    public Date getCreatedAt() {
        return createdAt;
    }

    public void setCreatedAt(Date createdAt) {
        this.createdAt = createdAt;
    }
    
    public AuthorRole getAuthorRole() {
        return authorRole;
    }

    public void setAuthorRole(AuthorRole authorRole) {
        this.authorRole = authorRole;
    }

    public List<KernelContent<?>> getItems() {
        return Collections.unmodifiableList(items);
    }

    public void setItems(List<KernelContent<?>> items) {
        this.items = items;
    }
}

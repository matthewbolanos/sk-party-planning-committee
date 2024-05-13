package com.partyplanning.lightingagent.models;

import org.springframework.data.annotation.Id;
import org.springframework.data.mongodb.core.mapping.Document;
import org.springframework.data.mongodb.core.mapping.Field;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.microsoft.semantickernel.services.KernelContent;
import com.microsoft.semantickernel.services.chatcompletion.AuthorRole;
import com.microsoft.semantickernel.services.chatcompletion.ChatMessageContent;
import java.util.Date;
import java.util.List;

@Document(collection = "MessageContents")
public class AssistantMessageContent<T> extends ChatMessageContent<T> {

    public AssistantMessageContent(String threadId, AuthorRole authorRole, String content) {
        super(authorRole, content);
        this.threadId = threadId;
    }

    public AssistantMessageContent(String threadId, AuthorRole authorRole, List<KernelContent<T>> items) {
        super(authorRole, items, null, null, null, null);
        this.threadId = threadId;
    }

    @Id
    private String id;

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

    // Constructors, getters, and setters

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
}

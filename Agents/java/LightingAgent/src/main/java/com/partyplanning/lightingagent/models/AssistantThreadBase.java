package com.partyplanning.lightingagent.models;

import org.bson.types.ObjectId;
import org.springframework.data.annotation.Id;
import org.springframework.data.mongodb.core.mapping.Document;
import org.springframework.data.mongodb.core.mapping.Field;

import java.util.Date;

@Document(collection = "Threads")
public class AssistantThreadBase {

    @Id
    private String id = ObjectId.get().toString();

    @Field("object")
    private String object = "thread";

    @Field("created_at")
    private Date createdAt = new Date();

    // Constructors, Getters, and Setters

    public AssistantThreadBase() {
    }

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

    public Date getCreatedAt() {
        return createdAt;
    }

    public void setCreatedAt(Date createdAt) {
        this.createdAt = createdAt;
    }
}

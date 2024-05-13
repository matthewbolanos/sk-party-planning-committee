package com.partyplanning.lightingagent.repositories;

import com.partyplanning.lightingagent.models.AssistantMessageContent;
import org.springframework.data.mongodb.repository.MongoRepository;

@SuppressWarnings("rawtypes")
public interface MessageRepository extends MongoRepository<AssistantMessageContent, String> {
    // Additional custom methods can be added here if needed
}

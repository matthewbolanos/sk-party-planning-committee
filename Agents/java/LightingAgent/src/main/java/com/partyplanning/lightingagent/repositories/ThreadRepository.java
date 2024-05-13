package com.partyplanning.lightingagent.repositories;

import com.partyplanning.lightingagent.models.AssistantThreadBase;
import org.springframework.data.mongodb.repository.MongoRepository;

public interface ThreadRepository extends MongoRepository<AssistantThreadBase, String> {
    // Additional custom methods can be added here if needed
}

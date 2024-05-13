package com.partyplanning.lightingagent.controllers;

import org.bson.types.ObjectId;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Sort;
import org.springframework.data.mongodb.core.MongoTemplate;
import org.springframework.data.mongodb.core.query.Criteria;
import org.springframework.data.mongodb.core.query.Query;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import com.partyplanning.lightingagent.models.AssistantMessageContent;
import com.partyplanning.lightingagent.models.AssistantThreadBase;
import com.partyplanning.lightingagent.models.AssistantMessageContentInputModel;

import java.net.URI;
import java.util.Date;
import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/api/threads/{threadId}/messages")
public class MessageController {

    @Autowired
    private MongoTemplate mongoTemplate;

    /**
     * Creates a new message in a specific thread.
     */
    @PostMapping
    public ResponseEntity<?> createMessage(@PathVariable String threadId, @RequestBody AssistantMessageContentInputModel messageInput) {
        if (threadId == null || threadId.isEmpty() || messageInput == null) {
            return ResponseEntity.badRequest().body("Thread ID and message are required.");
        }

        if (mongoTemplate.exists(Query.query(Criteria.where("id").is(threadId)), AssistantThreadBase.class)) {
            AssistantMessageContent newMessage = new AssistantMessageContent(threadId, messageInput.getRole(), messageInput.getContent(), null, null, null);
            newMessage.setCreatedAt(new Date());
            mongoTemplate.insert(newMessage);

            return ResponseEntity.created(URI.create("/api/threads/" + threadId + "/messages/" + newMessage.getId())).body(newMessage);
        } else {
            return ResponseEntity.notFound().build();
        }
    }

    /**
     * Retrieves a message by its ID within a specific thread.
     */
    @GetMapping("{id}")
    public ResponseEntity<AssistantMessageContent> retrieveMessage(@PathVariable String threadId, @PathVariable String id) {
        AssistantMessageContent message = mongoTemplate.findOne(
            Query.query(Criteria.where("_id").is(new ObjectId(id)).and("threadId").is(threadId)),
            AssistantMessageContent.class);

        if (message == null) {
            return ResponseEntity.notFound().build();
        }

        return ResponseEntity.ok(message);
    }

    /**
     * Retrieves all messages in a specific thread with optional pagination.
     */
    @GetMapping
    public ResponseEntity<Object> listMessages(
        @PathVariable String threadId,
        @RequestParam(defaultValue = "20") int limit,
        @RequestParam(defaultValue = "desc") String order,
        @RequestParam(required = false) String after,
        @RequestParam(required = false) String before) {

        Query query = new Query(Criteria.where("threadId").is(threadId));
        Sort sort = order.equalsIgnoreCase("asc") ? Sort.by("createdAt").ascending() : Sort.by("createdAt").descending();
        query.with(PageRequest.of(0, limit, sort));

        List<AssistantMessageContent> messages = mongoTemplate.find(query, AssistantMessageContent.class);
        return ResponseEntity.ok(messages);
    }

    /**
     * Updates an existing message within a specific thread.
     */
    @PutMapping("{id}")
    public ResponseEntity<?> modifyMessage(@PathVariable String threadId, @PathVariable String id) {
        return ResponseEntity.status(HttpStatus.METHOD_NOT_ALLOWED).body("Update operation is not supported.");
    }

    /**
     * Deletes a message by its ID and returns confirmation.
     */
    @DeleteMapping("{id}")
    public ResponseEntity<?> deleteMessage(@PathVariable String threadId, @PathVariable String id) {
        Query query = Query.query(Criteria.where("_id").is(new ObjectId(id)).and("threadId").is(threadId));
        if (mongoTemplate.remove(query, AssistantMessageContent.class).getDeletedCount() > 0) {
            return ResponseEntity.ok(Map.of("id", id, "object", "message.deleted", "deleted", true));
        }
        return ResponseEntity.status(HttpStatus.NOT_FOUND).body(Map.of("id", id, "object", "message.deleted", "deleted", false));
    }
}

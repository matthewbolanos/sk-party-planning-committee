package com.partyplanning.lightingagent.controllers;

import com.partyplanning.lightingagent.models.AssistantThreadBase;
import com.partyplanning.lightingagent.models.AssistantMessageContent;
import com.partyplanning.lightingagent.models.ThreadInputModel;
import com.partyplanning.lightingagent.repositories.ThreadRepository;

import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.media.ExampleObject;

import com.partyplanning.lightingagent.repositories.MessageRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.UUID;
import java.util.stream.Collectors;

@RestController
@RequestMapping("/api/threads")
public class ThreadController {

    private final ThreadRepository threadRepository;
    private final MessageRepository messageRepository;

    @Autowired
    public ThreadController(ThreadRepository threadRepository, MessageRepository messageRepository) {
        this.threadRepository = threadRepository;
        this.messageRepository = messageRepository;
    }

    @PostMapping
    @Operation(summary = "Create a new thread",
        requestBody = @io.swagger.v3.oas.annotations.parameters.RequestBody(
            required = true,
            content = @Content(
                mediaType = "application/json",
                schema = @Schema(implementation = ThreadInputModel.class),
                examples = {
                    @ExampleObject(
                        name = "Simple User Message",
                        value = "{\n" +
                                "  \"messages\": [\n" +
                                "    {\n" +
                                "      \"role\": \"user\",\n" +
                                "      \"content\": [\n" +
                                "        {\n" +
                                "          \"type\": \"text\",\n" +
                                "          \"text\": \"How does AI work? Explain it in simple terms.\"\n" +
                                "        }\n" +
                                "      ]\n" +
                                "    }\n" +
                                "  ]\n" +
                                "}"
                    )
                }
            )
        )
    )
    public ResponseEntity<AssistantThreadBase> createThread(@RequestBody ThreadInputModel input) {
        if (input == null) {
            return ResponseEntity.badRequest().body(null);
        }

        String threadId = UUID.randomUUID().toString();
        @SuppressWarnings({ "rawtypes", "unchecked" })
        List<AssistantMessageContent> messages = input.getMessages().stream()
                .map(message -> new AssistantMessageContent(threadId, message.getRole(), message.getContent()))
                .collect(Collectors.toList());

        AssistantThreadBase newThread = new AssistantThreadBase();
        newThread.setId(threadId);
        threadRepository.save(newThread);
        if (!messages.isEmpty()) {
            messageRepository.saveAll(messages);
        }

        return ResponseEntity.status(HttpStatus.CREATED).body(newThread);
    }

    @GetMapping("/{id}")
    public ResponseEntity<AssistantThreadBase> retrieveThread(@PathVariable String id) {
        return threadRepository.findById(id)
                .map(thread -> ResponseEntity.ok(thread))
                .orElse(ResponseEntity.notFound().build());
    }

    @PutMapping("/{id}")
    public ResponseEntity<Void> modifyThread(@PathVariable String id) {
        // As modifying is not supported, return 405 Method Not Allowed
        return ResponseEntity.status(HttpStatus.METHOD_NOT_ALLOWED).build();
    }

    @DeleteMapping("/{id}")
    public ResponseEntity<?> deleteThread(@PathVariable String id) {
        Optional<AssistantThreadBase> threadOptional = threadRepository.findById(id);

        if (!threadOptional.isPresent()) {
            Map<String, Object> response = new HashMap<>();
            response.put("id", id);
            response.put("object", "thread.deleted");
            response.put("deleted", false);
            return ResponseEntity.status(HttpStatus.NOT_FOUND).body(response);
        }

        AssistantThreadBase thread = threadOptional.get();
        threadRepository.delete(thread);
        Map<String, Object> response = new HashMap<>();
        response.put("id", thread.getId());
        response.put("object", "thread.deleted");
        response.put("deleted", true);

        return ResponseEntity.ok(response);
    }
}

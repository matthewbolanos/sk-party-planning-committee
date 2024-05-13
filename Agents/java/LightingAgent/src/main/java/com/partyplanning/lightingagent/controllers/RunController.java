import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.server.ResponseStatusException;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.data.mongodb.core.MongoTemplate;
import org.springframework.web.servlet.mvc.method.annotation.SseEmitter;

import com.partyplanning.lightingagent.models.AssistantThreadBase;
import com.partyplanning.lightingagent.models.AssistantThreadRun;
import com.partyplanning.lightingagent.services.LightingAgentRunService;
import com.partyplanning.lightingagent.utils.AssistantEventStreamUtility;

import org.springframework.http.ResponseEntity;
import java.io.IOException;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

@RestController
@RequestMapping("/api/threads/{threadId}/runs")
public class RunController {

    @Autowired
    private MongoTemplate mongoTemplate;

    @Autowired
    private LightingAgentRunService runService;

    @Autowired
    private AssistantEventStreamUtility assistantEventStreamUtility;

    @PostMapping
    public SseEmitter createRun(@PathVariable String threadId) {
        if (threadId == null || threadId.isEmpty()) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Thread ID is required.");
        }

        AssistantThreadBase thread = mongoTemplate.findById(threadId, AssistantThreadBase.class, "Threads");
        if (thread == null) {
            throw new ResponseStatusException(HttpStatus.NOT_FOUND, "Thread with ID '" + threadId + "' not found.");
        }

        AssistantThreadRun newRun = new AssistantThreadRun();
        newRun.setThreadId(threadId);
        newRun.setAssistantId("lighting-agent");

        SseEmitter emitter = new SseEmitter(Long.MAX_VALUE);
        ExecutorService sseMvcExecutor = Executors.newSingleThreadExecutor();
        sseMvcExecutor.execute(() -> {
            try {
                assistantEventStreamUtility.sendEvent(emitter, "thread.run.created", newRun);
                assistantEventStreamUtility.sendEvent(emitter, "thread.run.queued", newRun);
                assistantEventStreamUtility.sendEvent(emitter, "thread.run.in_progress", newRun);
                assistantEventStreamUtility.sendEvent(emitter, "thread.run.step.created", newRun);
                assistantEventStreamUtility.sendEvent(emitter, "thread.run.step.in_progress", newRun);

                runService.executeRun(newRun).forEach(event -> assistantEventStreamUtility.sendEvent(emitter, event, newRun));

                assistantEventStreamUtility.sendEvent(emitter, "thread.run.completed", newRun);
                assistantEventStreamUtility.sendEvent(emitter, "thread.run.step.completed", newRun);
                assistantEventStreamUtility.sendDoneEvent(emitter);

                emitter.complete();
            } catch (Exception e) {
                emitter.completeWithError(e);
            }
        });

        return emitter;
    }
}

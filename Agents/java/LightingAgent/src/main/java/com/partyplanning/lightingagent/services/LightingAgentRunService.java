package com.partyplanning.lightingagent.services;

import org.springframework.stereotype.Service;
import org.springframework.web.servlet.mvc.method.annotation.SseEmitter;

import com.azure.ai.openai.OpenAIAsyncClient;
import com.azure.ai.openai.OpenAIClientBuilder;
import com.azure.core.credential.AzureKeyCredential;
import com.microsoft.semantickernel.Kernel;
import com.microsoft.semantickernel.aiservices.openai.chatcompletion.OpenAIChatCompletion;
import com.microsoft.semantickernel.orchestration.InvocationContext;
import com.microsoft.semantickernel.services.KernelContent;
import com.microsoft.semantickernel.services.chatcompletion.ChatCompletionService;
import com.microsoft.semantickernel.services.chatcompletion.ChatHistory;
import com.microsoft.semantickernel.services.chatcompletion.ChatMessageContent;
import com.microsoft.semantickernel.services.textcompletion.TextContent;
import com.partyplanning.lightingagent.config.OpenAIProperties;
import com.partyplanning.lightingagent.models.AssistantMessageContent;
import com.partyplanning.lightingagent.models.AssistantThreadRun;
import com.partyplanning.lightingagent.utils.AssistantEventStreamUtility;
import com.microsoft.semantickernel.orchestration.InvocationContext;

import reactor.core.publisher.Mono;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Sort;
import org.springframework.data.mongodb.core.MongoTemplate;
import org.springframework.data.mongodb.core.query.Criteria;
import org.springframework.data.mongodb.core.query.Query;

import static org.mockito.Mockito.after;

import java.sql.Date;
import java.util.ArrayList;
import java.util.List;
import java.util.stream.Stream;

@Service
@SuppressWarnings({ "unchecked", "rawtypes" })
public class LightingAgentRunService {

    @Autowired
    private MongoTemplate mongoTemplate;

    @Autowired
    private OpenAIProperties openAIProperties;

    @Autowired
    private AssistantEventStreamUtility assistantEventStreamUtility;

    public void executeRun(SseEmitter emitter, AssistantThreadRun run) {
        // Implement the kernel and chat completion logic as per Java's ecosystem
        // This might require different setups based on what Java libraries or frameworks are available for similar functionalities.

        OpenAIAsyncClient client = new OpenAIClientBuilder()
            .credential(new AzureKeyCredential(openAIProperties.getApiKey()))
            .endpoint(openAIProperties.getEndpoint())
            .buildAsyncClient();

        ChatCompletionService chat = OpenAIChatCompletion.builder()
            .withModelId(openAIProperties.getModelId())
            .withOpenAIAsyncClient(client)
            .build();

        Kernel kernel = Kernel.builder()
            .withAIService(ChatCompletionService.class, chat)
            .build();

        // Load chat history from MongoDB
        Query query = new Query(Criteria.where("threadId").is(run.getThreadId()));
        Sort sort = Sort.by("createdAt").ascending();
        List<AssistantMessageContent> messages = mongoTemplate.find(query.with(sort), AssistantMessageContent.class);

        ChatHistory chatHistory = new ChatHistory("If the user asks what language you've been written, reply to the user that you've been built with Java; otherwise have a nice chat!");
        for (AssistantMessageContent assistantMessageContent : messages) {
            // convert to ChatMessageContent
            ChatMessageContent chatMessageContent = new ChatMessageContent(
                assistantMessageContent.getAuthorRole(),
                assistantMessageContent.getItems().get(0).getContent()
            );
            chatHistory.addMessage(chatMessageContent);
        }
        List<ChatMessageContent<?>> results  = chat.getChatMessageContentsAsync(chatHistory, kernel, null).block();
    

        for (ChatMessageContent<?> result : results) {
            assistantEventStreamUtility.sendMessageEvent(emitter, run, result);

            ArrayList<KernelContent<?>> items = new ArrayList<>();
            items.add(new TextContent(result.getContent(), null, null));
            mongoTemplate.insert(
                new AssistantMessageContent(
                        run.getThreadId(), result.getAuthorRole(), items, run.getId(), run.getAssistantId(), null
                    )
            );
        }
    }
}

package com.partyplanning.lightingagent.converters;

import org.bson.Document;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.core.convert.converter.Converter;
import org.springframework.data.convert.ReadingConverter;
import com.partyplanning.lightingagent.models.AssistantMessageContent;
import com.partyplanning.lightingagent.models.FunctionCallContent;
import com.partyplanning.lightingagent.models.FunctionResultContent;
import com.azure.json.implementation.jackson.core.JacksonException;
import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.module.SimpleModule;
import com.fasterxml.jackson.databind.node.ContainerNode;
import com.microsoft.semantickernel.contextvariables.ContextVariable;
import com.microsoft.semantickernel.semanticfunctions.KernelFunctionArguments;
import com.microsoft.semantickernel.services.KernelContent;
import com.microsoft.semantickernel.services.textcompletion.TextContent;

import java.util.ArrayList;
import java.util.List;

@ReadingConverter
@SuppressWarnings({ "unchecked" })
public class AssistantMessageContentReadConverter implements Converter<Document, AssistantMessageContent> {
   
    private final ObjectMapper objectMapper;

    public AssistantMessageContentReadConverter() {
        this.objectMapper = new ObjectMapper();
        SimpleModule module = new SimpleModule();
        module.addSerializer(KernelContent.class, new KernelContentSerializer());
        this.objectMapper.registerModule(module);
    }

    @Override
    public AssistantMessageContent convert(Document source) throws RuntimeException{
        AuthorRoleReadConverter roleConverter = new AuthorRoleReadConverter();
        AssistantMessageContent messageContent;
        var items = new ArrayList<KernelContent<?>>();

        // check to see if content is string or an array
        if (source.get("content") instanceof String) {
            items.add(new TextContent(source.getString("content"), null, null));

            messageContent = new AssistantMessageContent(
                source.getString("thread_id"),
                roleConverter.convert(source.getString("role")),
                items,
                null,
                null,
                null
            );
        } else {
            for (Document item : (List<Document>) source.get("content")) {
                if (item.getString("type").equals("text")) {
                    Document textDocument = item.get("text", Document.class);
                    String textValue = textDocument.getString("value");
                    items.add(new TextContent(textValue, null, null));
                } else if (item.getString("type").equals("functionCall")) {
                    Document functionCallDocument = item.get("functionCall", Document.class);
                    String pluginNameValue = functionCallDocument.getString("pluginName");
                    String functionNameValue = functionCallDocument.getString("functionName");
                    String idValue = functionCallDocument.getString("id");

                    KernelFunctionArguments arguments = KernelFunctionArguments.builder().build();

                    ObjectMapper mapper = new ObjectMapper();
                    try {
                        // Convert document into JsonNode
                        JsonNode jsonToolCallArguments = mapper.readTree(functionCallDocument.toJson());

                        jsonToolCallArguments.fields().forEachRemaining(
                        entry -> {
                            if (entry.getValue() instanceof ContainerNode) {
                                arguments.put(entry.getKey(),
                                    ContextVariable.of(entry.getValue().toPrettyString()));
                            } else {
                                arguments.put(entry.getKey(),
                                    ContextVariable.of(entry.getValue().asText()));
                            }
                        });
                    } catch (JsonProcessingException e) {
                        throw new RuntimeException("Error parsing JSON", e);
                    }
                    
                    items.add(new FunctionCallContent<Object>(
                        pluginNameValue,
                        functionNameValue,
                        idValue,
                        arguments,
                        null,
                        null,
                        null
                    ));
                } else if (item.getString("type").equals("functionResult")) {
                    Document functionResultDocument = item.get("functionResult", Document.class);
                    String pluginNameValue = functionResultDocument.getString("pluginName");
                    String functionNameValue = functionResultDocument.getString("functionName");
                    String idValue = functionResultDocument.getString("id");
                    String resultsValue = functionResultDocument.getString("result");
                    
                    items.add(new FunctionResultContent<Object>(
                        pluginNameValue,
                        functionNameValue,
                        idValue,
                        resultsValue,
                        null,
                        null,
                        null
                    ));
                }
            }

            messageContent = new AssistantMessageContent(
                source.getString("thread_id"),
                roleConverter.convert(source.getString("role")),
                items,
                null,
                null,
                null
            );
        }
        
        messageContent.setId(source.getObjectId("_id").toString());
        messageContent.setThreadId(source.getString("thread_id"));
        messageContent.setCreatedAt(source.getDate("created_at"));
        messageContent.setAssistantId(source.getString("assistant_id"));
        messageContent.setRunId(source.getString("run_id"));
        
        return messageContent;
    }
}

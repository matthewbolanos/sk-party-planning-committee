package com.partyplanning.lightingagent.converters;

import com.fasterxml.jackson.core.JsonGenerator;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializerProvider;
import com.fasterxml.jackson.databind.module.SimpleModule;
import com.fasterxml.jackson.databind.ser.std.StdSerializer;
import com.microsoft.semantickernel.services.KernelContent;
import com.partyplanning.lightingagent.models.AssistantMessageContent;

import java.io.IOException;

public class AssistantMessageContentSerializer extends StdSerializer<AssistantMessageContent> {

    private final ObjectMapper objectMapper;

    public AssistantMessageContentSerializer() {
        this(null);
    }

    public AssistantMessageContentSerializer(Class<AssistantMessageContent> t) {
        super(t);
        this.objectMapper = new ObjectMapper();
        SimpleModule module = new SimpleModule();
        module.addSerializer(KernelContent.class, new KernelContentSerializer());
        this.objectMapper.registerModule(module);
    }

    @Override
    public void serialize(AssistantMessageContent value, JsonGenerator gen, SerializerProvider provider) throws IOException {
        gen.writeStartObject();
        gen.writeStringField("id", value.getId());
        gen.writeStringField("thread_id", value.getThreadId());
        gen.writeObjectField("run_id", value.getRunId());
        gen.writeObjectField("assistant_id", value.getAssistantId());
        gen.writeNumberField("created_at", value.getCreatedAt().getTime());
        gen.writeStringField("role", value.getAuthorRole().toString().toLowerCase());
        gen.writeArrayFieldStart("content");
        for (KernelContent<?> content : value.getItems()) {
            objectMapper.writeValue(gen, content);
        }
        gen.writeEndArray();
        gen.writeEndObject();
    }
}

package com.partyplanning.lightingagent.converters;

import com.fasterxml.jackson.core.JsonGenerator;
import com.fasterxml.jackson.databind.SerializerProvider;
import com.fasterxml.jackson.databind.ser.std.StdSerializer;
import com.microsoft.semantickernel.services.KernelContent;
import com.microsoft.semantickernel.services.textcompletion.TextContent;
import com.partyplanning.lightingagent.models.AssistantMessageContent;

import java.io.IOException;

public class AssistantMessageContentSerializer extends StdSerializer<AssistantMessageContent> {

    public AssistantMessageContentSerializer() {
        this(null);
    }

    public AssistantMessageContentSerializer(Class<AssistantMessageContent> t) {
        super(t);
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
            gen.writeStartObject();

            // check if the content is an instance of TextContent
            if (content instanceof TextContent) {
                TextContent textContent = (TextContent) content;
                gen.writeStringField("type", "text");
                gen.writeFieldName("text");
                gen.writeStartObject();
                gen.writeStringField("value", textContent.getContent());
                gen.writeArrayFieldStart("annotations");
                // Assuming annotations need to be an empty array if null
                gen.writeEndArray();
                gen.writeEndObject();
            }

            gen.writeEndObject();
        }
        gen.writeEndArray();
        gen.writeEndObject();
    }
}

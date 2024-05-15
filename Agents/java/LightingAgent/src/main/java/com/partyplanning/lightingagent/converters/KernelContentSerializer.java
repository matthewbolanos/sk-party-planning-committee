package com.partyplanning.lightingagent.converters;

import com.fasterxml.jackson.core.JsonGenerator;
import com.fasterxml.jackson.databind.SerializerProvider;
import com.fasterxml.jackson.databind.ser.std.StdSerializer;
import com.microsoft.semantickernel.services.KernelContent;
import com.microsoft.semantickernel.services.textcompletion.TextContent;
import com.partyplanning.lightingagent.models.FunctionCallContent;
import com.partyplanning.lightingagent.models.FunctionResultContent;

import java.io.IOException;

@SuppressWarnings("rawtypes")
public class KernelContentSerializer extends StdSerializer<KernelContent> {

    public KernelContentSerializer() {
        super(KernelContent.class);
    }

    @Override
    public void serialize(KernelContent value, JsonGenerator gen, SerializerProvider provider) throws IOException {
        if (value instanceof TextContent) {
            serializeTextContent((TextContent) value, gen);
        } else if (value instanceof FunctionCallContent) {
            serializeFunctionCallContent((FunctionCallContent) value, gen);
        } else if (value instanceof FunctionResultContent) {
            serializeFunctionResultContent((FunctionResultContent) value, gen);
        } else {
            throw new IOException("Unsupported KernelContent type: " + value.getClass().getSimpleName());
        }
    }

    private void serializeTextContent(TextContent value, JsonGenerator gen) throws IOException {
        gen.writeStartObject();
        gen.writeStringField("type", "text");
        gen.writeObjectFieldStart("text"); // start "text" object
        gen.writeStringField("value", value.getContent()); // "value" field within "text" object
        gen.writeEndObject(); // end "text" object
        gen.writeEndObject(); // end main object
    }

    private void serializeFunctionCallContent(FunctionCallContent value, JsonGenerator gen) throws IOException {
        gen.writeStartObject();
        gen.writeStringField("type", "functionCall");
        gen.writeObjectFieldStart("functionCall"); // start "functionCall" object
        gen.writeStringField("pluginName", value.getPluginName()); // "pluginName" field within "functionCall" object
        gen.writeStringField("functionName", value.getFunctionName()); // "functionName" field within "functionCall" object
        gen.writeStringField("id", value.getId()); // "id" field within "functionCall" object
        gen.writeObjectField("arguments", value.getArguments()); // "arguments" field within "functionCall" object
        gen.writeEndObject(); // end "functionCall" object
        gen.writeEndObject(); // end main object
    }

    private void serializeFunctionResultContent(FunctionResultContent value, JsonGenerator gen) throws IOException {
        gen.writeStartObject();
        gen.writeStringField("type", "functionResult");
        gen.writeObjectFieldStart("functionResult"); // start "functionResult" object
        gen.writeStringField("pluginName", value.getPluginName()); // "pluginName" field within "functionResult" object
        gen.writeStringField("functionName", value.getFunctionName()); // "functionName" field within "functionResult" object
        gen.writeStringField("id", value.getId()); // "id" field within "functionResult" object
        gen.writeStringField("result", value.getResults()); // "results" field within "functionResult" object
        gen.writeEndObject(); // end "functionResult" object
        gen.writeEndObject(); // end main object
    }
}

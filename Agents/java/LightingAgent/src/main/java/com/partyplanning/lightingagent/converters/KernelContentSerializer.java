package com.partyplanning.lightingagent.converters;

import com.fasterxml.jackson.core.JsonGenerator;
import com.fasterxml.jackson.databind.SerializerProvider;
import com.fasterxml.jackson.databind.ser.std.StdSerializer;
import com.microsoft.semantickernel.services.KernelContent;
import com.microsoft.semantickernel.services.textcompletion.TextContent;
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
}

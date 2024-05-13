package com.partyplanning.lightingagent.converters;

import com.fasterxml.jackson.core.JsonParser;
import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.DeserializationContext;
import com.fasterxml.jackson.databind.JsonDeserializer;
import com.fasterxml.jackson.databind.JsonNode;
import com.microsoft.semantickernel.services.KernelContent;
import com.microsoft.semantickernel.services.textcompletion.TextContent;
import java.io.IOException;

public class KernelContentDeserializer extends JsonDeserializer<KernelContent<?>> {
    @Override
    public KernelContent<?> deserialize(JsonParser p, DeserializationContext ctxt) throws IOException, JsonProcessingException {
        JsonNode node = p.getCodec().readTree(p);
        String type = node.get("type").asText();
        switch (type) {
            case "text":
                return new TextContent(node.get("text").asText(), null, null);
            default:
                throw new JsonProcessingException("Unknown type: " + type, p.getCurrentLocation()) {};
        }
    }
}

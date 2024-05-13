package com.partyplanning.lightingagent.converters;

import com.fasterxml.jackson.core.JsonParser;
import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.DeserializationContext;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.deser.std.StdDeserializer;
import com.microsoft.semantickernel.services.KernelContent;
import com.microsoft.semantickernel.services.textcompletion.TextContent;
import com.partyplanning.lightingagent.models.AssistantMessageContent;
import com.partyplanning.lightingagent.models.AssistantMessageContentInputModel;

import java.io.IOException;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;

public class AssistantMessageContentInputModelDeserializer extends StdDeserializer<AssistantMessageContentInputModel> {

    public AssistantMessageContentInputModelDeserializer() {
        this(null);
    }

    public AssistantMessageContentInputModelDeserializer(Class<?> vc) {
        super(vc);
    }

    @Override
    public AssistantMessageContentInputModel deserialize(JsonParser jp, DeserializationContext ctxt) 
        throws IOException, JsonProcessingException {
        AuthorRoleReaderConverter roleConverter = new AuthorRoleReaderConverter();
        
        JsonNode node = jp.getCodec().readTree(jp);
        AssistantMessageContentInputModel content = new AssistantMessageContentInputModel();
        content.setRole(roleConverter.convert(node.get("role").asText()));
        
        // Handling the 'content' array
        if (node.has("content")) {
            ArrayList<KernelContent<?>> items = new ArrayList<>();

            // check if content is a string or an array
            if (node.get("content").isTextual()) {
                items.add(new TextContent(node.get("content").asText(), null, null));
            } else {
                for (JsonNode contentNode : node.get("content"))
                {
                    if (contentNode.get("type").asText().equals("text"))
                    {
                        JsonNode textNode = contentNode.get("text");
                        items.add(new TextContent(textNode.get("value").asText(), null, null));
                    }
                }
            }
            
            content.setContent(items);
        }

        return content;
    }
}

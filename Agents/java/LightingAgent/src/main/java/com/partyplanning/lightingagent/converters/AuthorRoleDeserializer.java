package com.partyplanning.lightingagent.converters;

import com.fasterxml.jackson.core.JsonParser;
import com.fasterxml.jackson.databind.DeserializationContext;
import com.fasterxml.jackson.databind.JsonDeserializer;
import com.microsoft.semantickernel.services.chatcompletion.AuthorRole;
import java.io.IOException;

public class AuthorRoleDeserializer extends JsonDeserializer<AuthorRole> {

    @Override
    public AuthorRole deserialize(JsonParser jsonParser, DeserializationContext context) throws IOException {
        if (!jsonParser.currentToken().isScalarValue()) {
            throw new IOException("Unexpected token type: " + jsonParser.currentToken());
        }

        String label = jsonParser.getValueAsString().toLowerCase();

        switch(label) {
            case "assistant":
                return AuthorRole.ASSISTANT;
            case "user":
                return AuthorRole.USER;
            case "system":
                return AuthorRole.SYSTEM;
            case "tool":
                return AuthorRole.TOOL;
            default:
                throw new IOException("Unexpected author role type: " + label);
        }
    }
}

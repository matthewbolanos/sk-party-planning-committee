package com.partyplanning.lightingagent.converters;

import org.springframework.core.convert.converter.Converter;
import org.springframework.data.convert.ReadingConverter;
import com.microsoft.semantickernel.services.chatcompletion.AuthorRole;

@ReadingConverter
public class AuthorRoleReadConverter implements Converter<String, AuthorRole> {
    public AuthorRole convert(String source) {
        switch(source) {
            case "assistant":
                return AuthorRole.ASSISTANT;
            case "user":
                return AuthorRole.USER;
            case "system":
                return AuthorRole.SYSTEM;
            case "tool":
                return AuthorRole.TOOL;
            default:
                throw new RuntimeException("Unexpected author role type: " + source);
        }
    }
}
package com.partyplanning.lightingagent.converters;

import org.springframework.core.convert.converter.Converter;
import org.springframework.data.convert.WritingConverter;
import com.microsoft.semantickernel.services.chatcompletion.AuthorRole;

@WritingConverter
public class AuthorRoleWriterConverter implements Converter<AuthorRole, String> {
    public String convert(AuthorRole source) {
        return source.toString();
    }
}
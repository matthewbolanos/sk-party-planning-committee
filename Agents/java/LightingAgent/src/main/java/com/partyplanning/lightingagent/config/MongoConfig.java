package com.partyplanning.lightingagent.config;

import org.springframework.core.convert.converter.Converter;
import org.springframework.data.convert.ReadingConverter;
import org.springframework.data.convert.WritingConverter;
import org.springframework.data.mongodb.core.MongoTemplate;
import org.springframework.data.mongodb.core.convert.MongoCustomConversions;
import org.springframework.context.annotation.Bean;
import com.microsoft.semantickernel.services.chatcompletion.AuthorRole;
import com.mongodb.client.MongoClient;
import com.mongodb.client.MongoClients;

import org.springframework.context.annotation.Configuration;
import java.util.Arrays;

@Configuration
public class MongoConfig {

    @Bean
    public MongoTemplate mongoTemplate() throws Exception {
        MongoClient mongoClient = MongoClients.create("mongodb://localhost:27017"); // Modify the URI as needed
        return new MongoTemplate(mongoClient, "PartyPlanning"); // Replace "yourDatabaseName" with the actual name of your database
    }

    @Bean
    public MongoCustomConversions customConversions() {
        return new MongoCustomConversions(Arrays.asList(new AuthorRoleReader(), new AuthorRoleWriter()));
    }

    @WritingConverter
    public static class AuthorRoleWriter implements Converter<AuthorRole, String> {
        public String convert(AuthorRole source) {
            return source.toString();
        }
    }

    @ReadingConverter
    public static class AuthorRoleReader implements Converter<String, AuthorRole> {
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
}

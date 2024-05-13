package com.partyplanning.lightingagent.config;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.module.SimpleModule;
import com.partyplanning.lightingagent.converters.AssistantMessageContentInputModelDeserializer;
import com.partyplanning.lightingagent.converters.AssistantMessageContentSerializer;
import com.partyplanning.lightingagent.converters.KernelContentDeserializer;
import com.partyplanning.lightingagent.converters.KernelContentSerializer;
import com.partyplanning.lightingagent.models.AssistantMessageContent;
import com.partyplanning.lightingagent.models.AssistantMessageContentInputModel;
import com.microsoft.semantickernel.services.KernelContent;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class JacksonConfig {

    @Bean
    public ObjectMapper objectMapper() {
        ObjectMapper mapper = new ObjectMapper();
        SimpleModule module = new SimpleModule();
        module.addSerializer(AssistantMessageContent.class, new AssistantMessageContentSerializer());
        module.addSerializer(KernelContent.class, new KernelContentSerializer());
        module.addDeserializer(KernelContent.class, new KernelContentDeserializer());
        module.addDeserializer(AssistantMessageContentInputModel.class, new AssistantMessageContentInputModelDeserializer());
        mapper.registerModule(module);
        return mapper;
    }
}

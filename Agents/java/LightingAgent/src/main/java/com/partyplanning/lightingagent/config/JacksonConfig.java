package com.partyplanning.lightingagent.config;

import com.fasterxml.jackson.databind.JavaType;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.module.SimpleModule;
import com.partyplanning.lightingagent.converters.KernelContentDeserializer;
import com.microsoft.semantickernel.services.KernelContent;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class JacksonConfig {

    @Bean
    public ObjectMapper objectMapper() {
        ObjectMapper mapper = new ObjectMapper();
        SimpleModule module = new SimpleModule();
        module.addDeserializer(KernelContent.class, new KernelContentDeserializer());
        mapper.registerModule(module);
        return mapper;
    }
}

package com.partyplanning.lightingagent.factories;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.springframework.core.env.MapPropertySource;
import org.springframework.core.env.PropertySource;
import org.springframework.core.io.support.DefaultPropertySourceFactory;
import org.springframework.core.io.support.EncodedResource;

import java.io.IOException;
import java.util.List;
import java.util.Map;
import java.util.HashMap;


public class ConfigFileJsonPropertySourceFactory extends DefaultPropertySourceFactory {

    @Override
    public PropertySource<?> createPropertySource(String name, EncodedResource resource) throws IOException {
        if (resource == null) {
            return new MapPropertySource(name, Map.of());
        }

        ObjectMapper mapper = new ObjectMapper();
        @SuppressWarnings("unchecked")
        Map<String, Object> properties = mapper.readValue(resource.getInputStream(), Map.class);

        return new MapPropertySource(name != null ? name : resource.getResource().getDescription(), properties);
    }
}

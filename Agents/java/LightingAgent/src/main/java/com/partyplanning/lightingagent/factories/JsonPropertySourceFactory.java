package com.partyplanning.lightingagent.factories;

import org.springframework.core.io.support.PropertySourceFactory;
import org.springframework.core.env.PropertiesPropertySource;
import org.springframework.core.env.PropertySource;
import org.springframework.core.io.support.EncodedResource;
import com.fasterxml.jackson.databind.ObjectMapper;
import java.util.Map;
import java.util.Properties;
import java.io.IOException;

public class JsonPropertySourceFactory implements PropertySourceFactory {

    @Override
    public PropertySource<?> createPropertySource(String name, EncodedResource resource) throws IOException {
        // Use a default name if none is provided
        if (name == null || name.isEmpty()) {
            name = resource.getResource().getFilename();
        }

        // Check if the filename is still not set and set a default name if necessary
        if (name == null || name.isEmpty()) {
            name = "jsonPropertySource";
        }

        @SuppressWarnings("unchecked")
        Map<String, Object> readValue = new ObjectMapper().readValue(resource.getInputStream(), Map.class);
        Properties properties = new Properties();
        flattenJsonMap(properties, readValue, null);
        return new PropertiesPropertySource(name, properties);
    }

    @SuppressWarnings("unchecked")
    private void flattenJsonMap(Properties properties, Map<String, Object> map, String path) {
        for (Map.Entry<String, Object> entry : map.entrySet()) {
            String key = path != null ? path + "." + entry.getKey() : entry.getKey();
            Object value = entry.getValue();
            if (value instanceof Map) {
                flattenJsonMap(properties, (Map<String, Object>) value, key);
            } else {
                properties.put(key, value != null ? value.toString() : "");
            }
        }
    }
}

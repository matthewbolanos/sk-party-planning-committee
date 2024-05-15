package com.partyplanning.lightingagent.services;

import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestTemplate;

import java.util.List;
import java.util.concurrent.CompletableFuture;

@Service
public class HealthCheckService {

    private final RestTemplate restTemplate;

    public HealthCheckService(RestTemplate restTemplate) {
        this.restTemplate = restTemplate;
    }

    public CompletableFuture<String> getHealthyEndpointAsync(List<String> endpoints) {
        return getHealthyEndpointAsync(endpoints, "/health");
    }

    public CompletableFuture<String> getHealthyEndpointAsync(List<String> endpoints, String healthCheckPath) {
        return CompletableFuture.supplyAsync(() -> {
            for (String endpoint : endpoints) {
                if (isEndpointHealthy(endpoint, healthCheckPath)) {
                    return endpoint;
                }
            }
            throw new RuntimeException("All endpoints are down.");
        });
    }

    private boolean isEndpointHealthy(String endpoint, String healthCheckPath) {
        try {
            ResponseEntity<String> response = restTemplate.getForEntity(endpoint + healthCheckPath, String.class);
            return response.getStatusCode().is2xxSuccessful();
        } catch (Exception e) {
            return false;
        }
    }
}

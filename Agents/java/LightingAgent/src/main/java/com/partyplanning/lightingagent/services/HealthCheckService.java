package com.partyplanning.lightingagent.services;

import org.springframework.stereotype.Service;
import org.springframework.web.reactive.function.client.WebClient;
import reactor.core.publisher.Mono;

import java.util.List;
import java.util.concurrent.CompletableFuture;

@Service
public class HealthCheckService {

    private final WebClient webClient;

    public HealthCheckService(WebClient webClient) {
        this.webClient = webClient;
    }

    public CompletableFuture<String> getHealthyEndpointAsync(List<String> endpoints) {
        return getHealthyEndpointAsync(endpoints, "/health");
    }

    public CompletableFuture<String> getHealthyEndpointAsync(List<String> endpoints, String healthCheckPath) {
        // for some reason the first endpoint always has "[" prepended and the last one has "]" appended
        // so we need to remove them
        endpoints.set(0, endpoints.get(0).substring(1));
        endpoints.set(endpoints.size() - 1, endpoints.get(endpoints.size() - 1).substring(0, endpoints.get(endpoints.size() - 1).length() - 1));

        return CompletableFuture.supplyAsync(() -> {
            for (String endpoint : endpoints) {
                if (isEndpointHealthy(endpoint, healthCheckPath).block()) {
                    return endpoint;
                }
            }
            throw new RuntimeException("All endpoints are down: "+ endpoints.toString());
        });
    }

    private Mono<Boolean> isEndpointHealthy(String endpoint, String healthCheckPath) {
        return webClient.get()
                .uri(endpoint + healthCheckPath)
                .retrieve()
                .toBodilessEntity()
                .map(response -> response.getStatusCode().is2xxSuccessful())
                .onErrorReturn(false);
    }
}

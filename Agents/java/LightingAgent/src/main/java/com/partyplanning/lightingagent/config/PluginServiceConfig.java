package com.partyplanning.lightingagent.config;

import java.util.List;

import com.fasterxml.jackson.annotation.JsonProperty;

public class PluginServiceConfig {
    @JsonProperty("Endpoints")
    private List<String> endpoints;

    public List<String> getEndpoints() {
        return endpoints;
    }

    public void setEndpoints(List<String> endpoints) {
        this.endpoints = endpoints;
    }
}
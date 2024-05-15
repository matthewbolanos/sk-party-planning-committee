package com.partyplanning.lightingagent.config;

import java.util.List;

public class PluginServiceConfig {
    public List<String> endpoints;

    public PluginServiceConfig() {
    }

    public List<String> getEndpoints() {
        return endpoints;
    }

    public void setEndpoints(List<String> endpoints) {
        this.endpoints = endpoints;
    }
}

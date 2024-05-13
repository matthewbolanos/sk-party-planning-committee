package com.partyplanning.lightingagent.config;

import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.PropertySource;
import com.partyplanning.lightingagent.factories.JsonPropertySourceFactory;

@Configuration
@PropertySource(value = "${openai.config.location}", factory = JsonPropertySourceFactory.class)
@ConfigurationProperties(prefix = "openai")
public class OpenAIProperties {

    private String deploymentType;
    private String apiKey;
    private String modelId;
    private String deploymentName;
    private String endpoint;
    private String orgId;

    // Getters and Setters
    public String getDeploymentType() {
        return deploymentType;
    }

    public void setDeploymentType(String deploymentType) {
        this.deploymentType = deploymentType;
    }

    public String getApiKey() {
        return apiKey;
    }

    public void setApiKey(String apiKey) {
        this.apiKey = apiKey;
    }

    public String getModelId() {
        return modelId;
    }

    public void setModelId(String modelId) {
        this.modelId = modelId;
    }

    public String getDeploymentName() {
        return deploymentName;
    }

    public void setDeploymentName(String deploymentName) {
        this.deploymentName = deploymentName;
    }

    public String getEndpoint() {
        return endpoint;
    }

    public void setEndpoint(String endpoint) {
        this.endpoint = endpoint;
    }

    public String getOrgId() {
        return orgId;
    }

    public void setOrgId(String orgId) {
        this.orgId = orgId;
    }
}

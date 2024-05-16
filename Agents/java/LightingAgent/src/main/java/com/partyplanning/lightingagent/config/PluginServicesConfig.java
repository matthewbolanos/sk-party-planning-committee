package com.partyplanning.lightingagent.config;

import java.util.HashMap;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.PropertySource;

import com.partyplanning.lightingagent.factories.ConfigFileJsonPropertySourceFactory;

@Configuration
@PropertySource(value = "${shared.config.location}", factory = ConfigFileJsonPropertySourceFactory.class)
@ConfigurationProperties(prefix = "pluginservices")
public class PluginServicesConfig extends HashMap<String, PluginServiceConfig> {

}

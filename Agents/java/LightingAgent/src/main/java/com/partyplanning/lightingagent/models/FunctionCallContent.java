// Copyright (c) Microsoft. All rights reserved.
package com.partyplanning.lightingagent.models;
import javax.annotation.Nullable;

import org.springframework.beans.factory.annotation.Autowired;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.microsoft.semantickernel.orchestration.FunctionResultMetadata;
import com.microsoft.semantickernel.semanticfunctions.KernelFunctionArguments;
import com.microsoft.semantickernel.services.KernelContent;

/**
 * Content from a text completion service.
 */
public class FunctionCallContent<T> extends KernelContent<T> {

    private String pluginName;
    private String functionName;
    private String id;
    private KernelFunctionArguments arguments;

    /**
     * Initializes a new instance of the {@code TextContent} class with a provided content, model
     * ID, and metadata.
     *
     * @param pluginName  The name of the plugin.
     * @param functionName  The name of the function.
     * @param id The ID of the function.
     * @param results The arguments of the function.
     */
    public FunctionCallContent(
        String pluginName,
        String functionName,
        String id,
        KernelFunctionArguments arguments,
        @Nullable T innerContent,
        @Nullable String modelId,
        @Nullable FunctionResultMetadata metadata) {
        super(innerContent, modelId, metadata);
        this.pluginName = pluginName;
        this.functionName = functionName;
        this.id = id;
        this.arguments = arguments;
    }

    /**
     * Gets the name of the plugin.
     *
     * @return The name of the plugin.
     */
    public String getPluginName() {
        return pluginName;
    }

    /**
     * Gets the name of the function.
     *
     * @return The name of the function.
     */
    public String getFunctionName() {
        return functionName;
    }

    /**
     * Gets the ID of the function.
     *
     * @return The ID of the function.
     */
    public String getId() {
        return id;
    }

    /**
     * Gets the arguments of the function.
     *
     * @return The arguments of the function.
     */
    public KernelFunctionArguments getArguments() {
        return arguments;
    }

    @Override
    @Nullable
    public java.lang.String getContent() {
        String argumentString = "";
        try {
            ObjectMapper objectMapper = new ObjectMapper();
            argumentString = objectMapper.writeValueAsString(arguments);
        } catch (Exception e) {
            argumentString = "Error converting arguments to string";
        }
        
        return pluginName + "-" + functionName + ": " + argumentString;
    }
}

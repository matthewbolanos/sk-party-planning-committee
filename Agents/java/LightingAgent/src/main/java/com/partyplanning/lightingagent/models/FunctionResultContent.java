// Copyright (c) Microsoft. All rights reserved.
package com.partyplanning.lightingagent.models;
import javax.annotation.Nullable;
import com.microsoft.semantickernel.orchestration.FunctionResultMetadata;
import com.microsoft.semantickernel.services.KernelContent;

/**
 * Content from a text completion service.
 */
public class FunctionResultContent<T> extends KernelContent<T> {

    private String pluginName;
    private String functionName;
    private String id;
    private String results;

    /**
     * Initializes a new instance of the {@code TextContent} class with a provided content, model
     * ID, and metadata.
     *
     * @param pluginName  The name of the plugin.
     * @param functionName  The name of the function.
     * @param id The ID of the function.
     * @param results The results of the function.
     */
    public FunctionResultContent(
        String pluginName,
        String functionName,
        String id,
        String results,
        @Nullable T innerContent,
        @Nullable String modelId,
        @Nullable FunctionResultMetadata metadata) {
        super(innerContent, modelId, metadata);
        this.pluginName = pluginName;
        this.functionName = functionName;
        this.id = id;
        this.results = results;
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
     * Gets the results of the function.
     *
     * @return The results of the function.
     */
    public String getResults() {
        return results;
    }

    @Override
    public String toString() {
        return results;
    }

    @Override
    @Nullable
    public java.lang.String getContent() {
        return results;
    }
}

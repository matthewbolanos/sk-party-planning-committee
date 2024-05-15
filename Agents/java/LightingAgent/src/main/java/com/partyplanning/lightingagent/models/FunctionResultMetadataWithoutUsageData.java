package com.partyplanning.lightingagent.models;

import java.time.OffsetDateTime;

import com.microsoft.semantickernel.contextvariables.CaseInsensitiveMap;
import com.microsoft.semantickernel.contextvariables.ContextVariable;
import com.microsoft.semantickernel.orchestration.FunctionResultMetadata;

public class FunctionResultMetadataWithoutUsageData extends FunctionResultMetadata {
    
    public FunctionResultMetadata build(
        String id,
        OffsetDateTime createdAt) {

        CaseInsensitiveMap<ContextVariable<?>> metadata = new CaseInsensitiveMap<>();
        metadata.put(ID, ContextVariable.of(id));
        metadata.put(CREATED_AT, ContextVariable.of(createdAt));

        return new FunctionResultMetadata(metadata);
    }
}

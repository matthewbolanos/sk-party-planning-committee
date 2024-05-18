package com.partyplanning.lightingagent.converters;

import java.util.ArrayList;
import java.util.List;

import org.bson.Document;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.core.convert.converter.Converter;
import org.springframework.data.convert.WritingConverter;
import com.partyplanning.lightingagent.models.AssistantMessageContent;
import com.partyplanning.lightingagent.models.FunctionCallContent;
import com.partyplanning.lightingagent.models.FunctionResultContent;
import com.microsoft.semantickernel.services.KernelContent;
import com.microsoft.semantickernel.services.textcompletion.TextContent;

@WritingConverter
public class AssistantMessageContentWriteConverter implements Converter<AssistantMessageContent, Document> {

    private final KernelFunctionArgumentsWriteConverter kernelFunctionArgumentsWriteConverter;

    public AssistantMessageContentWriteConverter(KernelFunctionArgumentsWriteConverter kernelFunctionArgumentsWriteConverter) {
        this.kernelFunctionArgumentsWriteConverter = kernelFunctionArgumentsWriteConverter;
    }

    @Override
    public Document convert(AssistantMessageContent source) {
        Document doc = new Document();
        doc.put("_id", new org.bson.types.ObjectId(source.getId()));
        doc.put("thread_id", source.getThreadId());
        doc.put("created_at", source.getCreatedAt());
        doc.put("assistant_id", source.getAssistantId());
        doc.put("run_id", source.getRunId());

        // Assume Role and items require similar serialization logic to .NET's version
        doc.put("role", source.getAuthorRole().toString().toLowerCase());
        List<Document> items = new ArrayList<Document>();

        for (KernelContent<?> item : source.getItems()) {
            Document itemDoc = new Document();

            if (item instanceof TextContent) {
                TextContent textContent = (TextContent) item;
                itemDoc.put("type", "text");
                Document textDocument = new Document();
                textDocument.put("value", textContent.getContent());
                textDocument.put("annotations", new ArrayList<>());
                itemDoc.put("text", textDocument);
            } else if (item instanceof FunctionCallContent) {
                FunctionCallContent<?> functionCallContent = (FunctionCallContent<?>) item;
                Document argumentsDocument = kernelFunctionArgumentsWriteConverter.convert(functionCallContent.getArguments());
            
                itemDoc.put("type", "functionCall");
                Document functionCallDocument = new Document();
                functionCallDocument.put("id", functionCallContent.getId());
                functionCallDocument.put("pluginName", functionCallContent.getPluginName());
                functionCallDocument.put("functionName", functionCallContent.getFunctionName());
                functionCallDocument.put("arguments", argumentsDocument);
                itemDoc.put("functionCall", functionCallDocument);
            } else if (item instanceof FunctionResultContent) {
                FunctionResultContent<?> functionResultContent = (FunctionResultContent<?>) item;
                itemDoc.put("type", "functionResult");
                Document functionResultDocument = new Document();
                functionResultDocument.put("id", functionResultContent.getId());
                functionResultDocument.put("pluginName", functionResultContent.getPluginName());
                functionResultDocument.put("functionName", functionResultContent.getFunctionName());
                functionResultDocument.put("result", functionResultContent.getContent());
                itemDoc.put("functionResult", functionResultDocument);
            }
            

            items.add(itemDoc);
        }

        doc.put("content", items);
        return doc;
    }
}

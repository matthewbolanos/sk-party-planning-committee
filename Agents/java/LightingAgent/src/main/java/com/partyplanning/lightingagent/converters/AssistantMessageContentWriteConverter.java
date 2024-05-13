package com.partyplanning.lightingagent.converters;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;

import org.bson.Document;
import org.springframework.core.convert.converter.Converter;
import org.springframework.data.convert.WritingConverter;
import com.partyplanning.lightingagent.models.AssistantMessageContent;
import com.microsoft.semantickernel.services.KernelContent;
import com.microsoft.semantickernel.services.textcompletion.TextContent;

@WritingConverter
public class AssistantMessageContentWriteConverter implements Converter<AssistantMessageContent, Document> {
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
                itemDoc.put("text", new Document(new HashMap<>(){
                    {
                        put("value", textContent.getContent());
                        put("annotations", new ArrayList<>());
                    }
                }));
            }

            items.add(itemDoc);
        }

        doc.put("content", items);
        return doc;
    }
}

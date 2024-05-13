package com.partyplanning.lightingagent.converters;

import org.bson.Document;
import org.springframework.core.convert.converter.Converter;
import org.springframework.data.convert.ReadingConverter;
import com.partyplanning.lightingagent.models.AssistantMessageContent;
import com.microsoft.semantickernel.services.KernelContent;
import com.microsoft.semantickernel.services.textcompletion.TextContent;

import java.util.ArrayList;
import java.util.List;

@ReadingConverter
@SuppressWarnings({ "unchecked" })
public class AssistantMessageContentReadConverter implements Converter<Document, AssistantMessageContent> {
    @Override
    public AssistantMessageContent convert(Document source) {
        AuthorRoleReaderConverter roleConverter = new AuthorRoleReaderConverter();
        AssistantMessageContent messageContent;
        var items = new ArrayList<KernelContent<?>>();

        // check to see if content is string or an array
        if (source.get("content") instanceof String) {
            items.add(new TextContent(source.getString("content"), null, null));

            messageContent = new AssistantMessageContent(
                source.getString("thread_id"),
                roleConverter.convert(source.getString("role")),
                items,
                null,
                null,
                null
            );
        } else {
            for (Document item : (List<Document>) source.get("content")) {
                if (item.getString("type").equals("text")) {
                    // Access the 'text' document field directly
                    Document textDocument = item.get("text", Document.class);
                    // Now fetch the 'value' field from the 'text' document
                    String textValue = textDocument.getString("value");
                    // Assuming TextContent is your custom class to handle this data
                    items.add(new TextContent(textValue, null, null));
                }
            }

            messageContent = new AssistantMessageContent(
                source.getString("thread_id"),
                roleConverter.convert(source.getString("role")),
                items,
                null,
                null,
                null
            );
        }
        
        messageContent.setId(source.getObjectId("_id").toString());
        messageContent.setThreadId(source.getString("thread_id"));
        messageContent.setCreatedAt(source.getDate("created_at"));
        messageContent.setAssistantId(source.getString("assistant_id"));
        messageContent.setRunId(source.getString("run_id"));
        
        return messageContent;
    }
}

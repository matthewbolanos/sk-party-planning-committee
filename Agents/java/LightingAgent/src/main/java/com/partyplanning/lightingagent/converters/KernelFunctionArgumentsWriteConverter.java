package com.partyplanning.lightingagent.converters;

import org.bson.Document;
import org.springframework.core.convert.converter.Converter;
import org.springframework.data.convert.WritingConverter;

import com.github.jknack.handlebars.internal.text.StringEscapeUtils;
import com.microsoft.semantickernel.semanticfunctions.KernelFunctionArguments;

@WritingConverter
public class KernelFunctionArgumentsWriteConverter implements Converter<KernelFunctionArguments, Document> {
    
    @Override
    public Document convert(KernelFunctionArguments source) {
        Document doc = new Document();

        // Assume KernelFunctionArguments has a list of ContextVariable objects
        for (var key : source.keySet()) {
            String value = source.get(key).toPromptString();

            // HTML decode the value
            doc.put(key, StringEscapeUtils.unescapeHtml4(value));
        }
        
        return doc;
    }
}

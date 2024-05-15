package com.partyplanning.lightingagent.config;

import org.springframework.data.mongodb.MongoDatabaseFactory;
import org.springframework.data.mongodb.core.MongoTemplate;
import org.springframework.data.mongodb.core.SimpleMongoClientDatabaseFactory;
import org.springframework.data.mongodb.core.convert.DbRefResolver;
import org.springframework.data.mongodb.core.convert.DefaultDbRefResolver;
import org.springframework.data.mongodb.core.convert.DefaultMongoTypeMapper;
import org.springframework.data.mongodb.core.convert.MappingMongoConverter;
import org.springframework.data.mongodb.core.convert.MongoCustomConversions;
import org.springframework.data.mongodb.core.mapping.MongoMappingContext;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.context.annotation.Bean;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.mongodb.client.MongoClient;
import com.mongodb.client.MongoClients;
import com.partyplanning.lightingagent.converters.AssistantMessageContentReadConverter;
import com.partyplanning.lightingagent.converters.AssistantMessageContentWriteConverter;
import com.partyplanning.lightingagent.converters.AuthorRoleReaderConverter;
import com.partyplanning.lightingagent.converters.AuthorRoleWriterConverter;

import org.springframework.context.annotation.Configuration;
import java.util.Arrays;

@Configuration
public class MongoConfig {

    @Bean
    public MongoTemplate mongoTemplate(MongoDatabaseFactory mongoDatabaseFactory, MongoMappingContext context) throws Exception {
        DbRefResolver dbRefResolver = new DefaultDbRefResolver(mongoDatabaseFactory);
        MappingMongoConverter converter = new MappingMongoConverter(dbRefResolver, context);
        converter.setTypeMapper(new DefaultMongoTypeMapper(null)); // This tells Spring not to save the '_class' attribute
        converter.setCustomConversions(customConversions());
        converter.afterPropertiesSet();
        return new MongoTemplate(mongoDatabaseFactory, converter);
    }

    @Bean
    public MongoDatabaseFactory mongoDatabaseFactory() {
        MongoClient mongoClient = MongoClients.create("mongodb://localhost:27017");
        return new SimpleMongoClientDatabaseFactory(mongoClient, "PartyPlanning");
    }

    @Bean
    public MongoCustomConversions customConversions() {
        return new MongoCustomConversions(Arrays.asList(
            new AuthorRoleReaderConverter(),
            new AuthorRoleWriterConverter(),
            new AssistantMessageContentReadConverter(),
            new AssistantMessageContentWriteConverter()
        ));
    }
}

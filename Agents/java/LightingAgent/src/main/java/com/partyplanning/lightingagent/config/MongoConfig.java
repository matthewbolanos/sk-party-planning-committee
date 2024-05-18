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
import org.springframework.context.annotation.Bean;

import com.mongodb.client.MongoClient;
import com.mongodb.client.MongoClients;
import com.partyplanning.lightingagent.converters.AssistantMessageContentReadConverter;
import com.partyplanning.lightingagent.converters.AssistantMessageContentWriteConverter;
import com.partyplanning.lightingagent.converters.AuthorRoleReadConverter;
import com.partyplanning.lightingagent.converters.AuthorRoleWriteConverter;
import com.partyplanning.lightingagent.converters.KernelFunctionArgumentsWriteConverter;

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
        String mongoUri = System.getenv("MONGO_URI");
        MongoClient mongoClient = MongoClients.create(mongoUri);
        return new SimpleMongoClientDatabaseFactory(mongoClient, "PartyPlanning");
    }

    @Bean
    public MongoCustomConversions customConversions() {
        return new MongoCustomConversions(Arrays.asList(
            new AuthorRoleReadConverter(),
            new AuthorRoleWriteConverter(),
            new AssistantMessageContentReadConverter(),
            new AssistantMessageContentWriteConverter(new KernelFunctionArgumentsWriteConverter())
        ));
    }
}

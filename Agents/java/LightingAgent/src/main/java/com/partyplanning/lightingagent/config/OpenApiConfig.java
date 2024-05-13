// package com.partyplanning.lightingagent.config;

// import io.swagger.v3.oas.annotations.media.Schema;

// import org.springdoc.core.GroupedOpenApi;
// import org.springdoc.core.customizers.OpenApiCustomiser;
// import org.springframework.context.annotation.Bean;
// import org.springframework.context.annotation.Configuration;
// import io.swagger.v3.oas.models.media.StringSchema;

// @Configuration
// public class OpenApiConfig {
//     @Bean
//     public GroupedOpenApi publicApi() {
//         return GroupedOpenApi.builder()
//             .group("spring")
//             .packagesToScan("com.partyplanning.lightingagent.controllers")
//             .build();
//     }

//     @Schema(name = "AssistantThread", description = "Represents a thread resource", example = "{\"id\":\"663ef31ed04a068ed8ed1fef\",\"object\":\"thread\",\"created_at\":5250196001451743000}")
//     public class AssistantThreadSchema {
//     }

//     @Bean
//     public OpenApiCustomiser authorRoleCustomiser() {
//         return openApi -> {
//             openApi.getComponents().addSchemas("AuthorRole", new StringSchema().example("user"));
//         };
//     }
// }

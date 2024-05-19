# Use a base image with Maven to build the application
FROM maven:3.9.6-sapmachine-21 AS build

# Set the working directory for the build
WORKDIR /app

RUN mkdir -p /app/Agents/java/LightingAgent

# Copy the pom.xml file and download dependencies
COPY Agents/java/LightingAgent/pom.xml /app/Agents/java/LightingAgent/
WORKDIR /app/Agents/java/LightingAgent
RUN mvn dependency:go-offline

# Copy the rest of the application source code and build the application
COPY Agents/java/LightingAgent/src /app/Agents/java/LightingAgent/src/
WORKDIR /app/Agents/java/LightingAgent
RUN mvn clean package -DskipTests

# Use a base image with Java to run the application
FROM openjdk:21-jdk-slim

# Copy the JAR file from the build stage
COPY --from=build /app/Agents/java/LightingAgent/target/lighting-agent-0.0.1-SNAPSHOT.jar ./lighting-agent.jar

# Copy config.json to the expected location
COPY config.json ../../../config.json
COPY PluginResources ../../../PluginResources/

# Expose the port your application will run on
EXPOSE 8080

# Install curl in the base image
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Define the command to run the application
ENTRYPOINT ["java", "--add-opens", "java.base/java.nio.charset=ALL-UNNAMED", "-Dmanagement.server.port=8080", "-jar", "lighting-agent.jar"]


package com.partyplanning.lightingagent.services;

import org.springframework.stereotype.Service;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;

@Service
public class OpenApiSpecLoaderService {

    private final Path directoryPath = Paths.get("../../../PluginResources/OpenApiPlugins/");

    public String loadSpecAsString(String fileName) throws IOException {
        Path filePath = directoryPath.resolve(fileName);
        if (!Files.exists(filePath) || !Files.isRegularFile(filePath)) {
            throw new IOException("File does not exist or is not a regular file: " + filePath);
        }
        return Files.readString(filePath);
    }
}

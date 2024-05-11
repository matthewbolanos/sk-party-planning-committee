# Semantic Kernel Home Automation sample

This is a sample project that demonstrates how to use the Semantic Kernel to create a home automation system across the three different SDKs: Python, .NET, and Java.

## Getting Started

### Setting up your configuration

Depending on the SDK you are using, you will need to set up your secrets in the respective configuration file, environment, or secret manager.

#### Python

Create a `.env` file in the root of the project and add the following:

```bash
# Choose the following options based on your deployment type
#  - AzureOpenAI: Used if you are using Azure's OpenAI's service
#  - OpenAI: Used if you are using OpenAI's service
#  - Other: Used if you are using another deployment (e.g., Ollama) that provides an OpenAI API
DEPLOYMENT_TYPE=AzureOpenAI
OPENAI_API_KEY=your-api-key
MODEL_ID=your-model-id

# Set if you are using AzureOpenAI
# The deployment name may differ from the model name
DEPLOYMENT_NAME=your-models-deployment-name

# Set if you are using AzureOpenAI or Other
ENDPOINT=your-endpoint

# Set if you are using OpenAI
ORG_ID=your-org-id
```

#### .NET

With .NET, it is recommended to use .NET Secret Manager to store your secrets. You can set up your secrets by running the following commands in the root of the project:

```bash
# Choose the following options based on your deployment type
#  - AzureOpenAI: Used if you are using Azure's OpenAI's service
#  - OpenAI: Used if you are using OpenAI's service
#  - Other: Used if you are using another deployment (e.g., Ollama) that provides an OpenAI API
dotnet user-secrets set "OpenAI:DeploymentType" "AzureOpenAI" --project Agents/dotnet/SharedConfig
dotnet user-secrets set "OpenAI:ApiKey" "your-api-key" --project Agents/dotnet/SharedConfig
dotnet user-secrets set "OpenAI:ModelId" "your-model-id" --project Agents/dotnet/SharedConfig

# Set if you are using AzureOpenAI
# The deployment name may differ from the model name
dotnet user-secrets set "OpenAI:DeploymentName" "your-models-deployment-name" --project Agents/dotnet/SharedConfig

# Set if you are using AzureOpenAI or Other
dotnet user-secrets set "OpenAI:Endpoint" "your-endpoint" --project Agents/dotnet/SharedConfig

# Set if you are using OpenAI and you are using an organization
dotnet user-secrets set "OpenAI:OrgId" "your-org-id" --project Agents/dotnet/SharedConfig
```

If you want to use _appsettings.json_ instead, you can add the following to _/Agents/dotnet/SharedConfig/sharedsettings.json_ so that the configuration is used by all the services (alternatively, you can duplicate this information in each of the _appsettings.json_ files under /Agents/dotnet). To get started, copy _/Agents/dotnet/SharedConfig/sharedsettings.sample.json_ and populate it with your configuration.

```json
{
    "OpenAI": {
        // Choose the following options based on your deployment type
        //  - AzureOpenAI: Used if you are using Azure's OpenAI's service
        //  - OpenAI: Used if you are using OpenAI's service
        //  - Other: Used if you are using another deployment (e.g., Ollama) that provides an OpenAI API
        "DeploymentType": "AzureOpenAI",
        "ApiKey": "your-api-key",
        "ModelId": "your-model-id",

        // Set if you are using AzureOpenAI
        // The deployment name may differ from the model name
        "DeploymentName": "your-models-deployment-name",

        // Set if you are using AzureOpenAI or Other
        "Endpoint": "your-endpoint",

        // Set if you are using OpenAI
        "OrgId": "your-org-id"
    }
}
```
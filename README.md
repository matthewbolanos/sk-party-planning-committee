# Semantic Kernel Party Planning Committee solution

This is a sample project that demonstrates how to use agents built with Semantic Kernel to create
a home automation system across three different languages (Python, .NET, and Java). Together, the
agents built in this solution will help you throw an amazing party!

> [!IMPORTANT]  
> The party planning committee is just now getting started, we're eager to bring more agents into
> the mix so they can help us plan the best parties ever! 🎉 (PRs are welcome)

// Include gif of the app in action

## What's included?!
This sample has it all! (Or at least it tries to). Here's what you can expect to find in this sample:
- A retro inspired console application to interact with your agents across _all three languages_ (wow)
- Controllers that mimic the OpenAI Assistant's API in Python, .NET, and Java with the help of Semantic Kernel
- Plugin services that provide your agents the ability to complete party planning tasks
- A MongoDB database to store your chats and party planning data

### Available agents
| Agent          | Description                                  | Python | .NET | Java |
| -------------- | -------------------------------------------- | ------ | ---- | ---- |
| Lighting Agent | Controls (and syncs) the lights in your home | ✅     | ✅   | ✅    |
| DJ Agent       | Synthesizes music on the fly for your party  | ✅     |      |      |
| Security Agent | Keeps your home safe from party crashers     | ✅     |      |      |

### Available plugins
All plugin services are .NET based, but the plugins themselves are language agnostic. 
This means any of your agents can use these plugins!

| Plugin                  | Description                                             |
| ----------------------- | ------------------------------------------------------- |
| Home Plugin             | Provides access to your home and rooms                  |
| Light Plugin            | Provides access to your lights                          |
| Color Theme Plugin      | Provides access to color themes  (powered by Weaviate!) |
| Music Generation Plugin | Allows agents to make new music on the fly              |
| Synchronization Plugin  | Syncs your devices with music                           |
| Speaker Plugin          | Provides access to your speakers                        |

## Getting Started
Getting started is easy! Just run the following command<del>s</del> to get your party planning
committee up and running.

```bash
docker-compose up --detach --build && docker-compose exec -it ui python main.py  
```

Wow! So easy! 🎉

Once the app is running, you'll be prompted to provide the required configuration
(e.g., API keys and LLM endpoints) for your agents.

Continue reading if you would like to set up your configuration before running the app.

### Setting up your configuration (pre-deployment)
If you would like to set up your configuration before running the app so that you aren't
prompted every time you start the app, you can follow the instructions below.

Depending on the SDK you are using, you will need to set up your secrets in the
respective configuration file, environment, or secret manager.

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

If you want to use something like _appsettings.json_ instead, you can add the following to _/Agents/dotnet/SharedConfig/sharedsettings.json_ so that the configuration is used by all the services (alternatively, you can duplicate this information in each of the _appsettings.json_ files under /Agents/dotnet). To get started, copy _/Agents/dotnet/SharedConfig/sharedsettings.sample.json_ and populate it with your configuration.

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
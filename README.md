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

If you ever want to start fresh (i.e., reset the database), you can run the following command:

```bash
docker-compose down --volumes
```

Continue reading if you would like to set up your configuration before running the app.

### Setting up your configuration (pre-deployment)
If you would like to set up your configuration before running the app so that you aren't
prompted every time you start the app, you can follow the instructions below (no matter
what language you are using!).


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
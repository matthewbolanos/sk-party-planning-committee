import json
from typing import List
import aiohttp
import httpx
from semantic_kernel.kernel import Kernel
from semantic_kernel.contents import TextContent
from semantic_kernel.contents.chat_message_content import ITEM_TYPES, AuthorRole, ChatMessageContent
from semantic_kernel.contents.chat_history import ChatHistory
from semantic_kernel.connectors.ai.function_call_behavior import FunctionCallBehavior
from semantic_kernel.contents.streaming_chat_message_content import StreamingChatMessageContent
from semantic_kernel.connectors.ai.open_ai import (
    AzureChatCompletion,
    OpenAIChatCompletion,
    OpenAIChatPromptExecutionSettings
)
from semantic_kernel.connectors.openapi_plugin.openapi_function_execution_parameters import (
    OpenAPIFunctionExecutionParameters,
)
from semantic_kernel.connectors.ai.chat_completion_client_base import ChatCompletionClientBase
from fastapi import Depends
from services.health_check_service import HealthCheckService
from models.assistant_message_content import AssistantMessageContent
from models.config import Config
from database_manager import DatabaseManager, get_database_manager
from models.assistant_thread_run import AssistantThreadRun
from utilities.assistant_event_stream_utility import AssistantEventStreamService
from utilities.chat_message_conversion_utility import process_messages
from semantic_kernel.functions.kernel_arguments import KernelArguments

class RunService:
    async def execute_run_async(
            self,
            run: AssistantThreadRun,
            event_stream_utility: AssistantEventStreamService,
            db_manager: DatabaseManager,
            http_client: httpx.AsyncClient
        ):

        # Load variables from config.json at the root of the solution
        with open('../../../config.json') as file:
            json_data = json.load(file)
            config: Config = Config(**json_data)
        deployment_type, api_key, ai_model_id, deployment_name, endpoint, org_id = config.openai.model_dump().values()
        plugin_services = config.plugin_services
        health_check_service = HealthCheckService(http_client)

        # Create kernel
        kernel: Kernel = Kernel()

        # Add AI services
        if (deployment_type == "AzureOpenAI"):
            kernel.add_service(
                AzureChatCompletion(
                    deployment_name=deployment_name,
                    api_key=api_key,
                    endpoint=endpoint,
                ),
            )
        elif (deployment_type == "OpenAI"):
            kernel.add_service(
                OpenAIChatCompletion(
                    api_key=api_key,
                    ai_model_id=ai_model_id,
                    org_id=org_id, # org_id is optional
                ),
            )

        # Add plugin services

        # Load the Light plugin
        light_service_endpoint = await health_check_service.get_healthy_endpoint(plugin_services['LightService'].endpoints)
        kernel.add_plugin_from_openapi(
            plugin_name="light_plugin",
            openapi_document_path="../../../PluginResources/OpenApiPlugins/LightPlugin.swagger.json",
            execution_settings=OpenAPIFunctionExecutionParameters(
                http_client=http_client,
                server_url_override=light_service_endpoint,
                enable_payload_namespacing=True,
            ),
        )


        # Load the Scene plugin
        scene_service_endpoint = await health_check_service.get_healthy_endpoint(plugin_services['SceneService'].endpoints)
        kernel.add_plugin_from_openapi(
            plugin_name="scene_plugin",
            openapi_document_path="../../../PluginResources/OpenApiPlugins/ScenePlugin.swagger.json",
            execution_settings=OpenAPIFunctionExecutionParameters(
                http_client=http_client,
                server_url_override=scene_service_endpoint,
                enable_payload_namespacing=True,
            ),
        )

        # Load the Speaker plugin
        speaker_service_endpoint = await health_check_service.get_healthy_endpoint(plugin_services['SpeakerService'].endpoints)
        kernel.add_plugin_from_openapi(
            plugin_name="speaker_plugin",
            openapi_document_path="../../../PluginResources/OpenApiPlugins/SpeakerPlugin.swagger.json",
            execution_settings=OpenAPIFunctionExecutionParameters(
                http_client=http_client,
                server_url_override=speaker_service_endpoint,
                enable_payload_namespacing=True,
            ),
        )

        # Load all the messages (chat history) from MongoDB using the thread ID and sort them by creation date
        messages: List[AssistantMessageContent] = await db_manager.messages_collection.find(
            {"thread_id": run.thread_id}
        ).sort("created_at").to_list(None)
        history = ChatHistory(
            system_message="If the user asks what language you've been written, reply to the user that you've been built with Python; otherwise have a nice chat! As an fyi, the current user is a developing you, so be forthcoming with any of the underlying tool calls your making in case they ask so they can debug.",
            messages=[AssistantMessageContent.from_bson(message) for message in messages]
        )
        messageCount = len(messages);

        # Invoke the chat completion service
        chatCompletion: ChatCompletionClientBase = kernel.get_service(type=ChatCompletionClientBase)
        results = chatCompletion.get_streaming_chat_message_contents(
            chat_history=history,
            settings=OpenAIChatPromptExecutionSettings(
                function_call_behavior=FunctionCallBehavior.AutoInvokeKernelFunctions()
            ),
            kernel=kernel,
            arguments=KernelArguments()
        )

        # Return the results as a stream 
        completeMessage = ""
        async for result in results:
            completeMessage += result[0].content

            # Send the message events to the client
            events = event_stream_utility.create_message_event(run, result[0])
            for event in events:
                yield event
        history.add_assistant_message(completeMessage)

        newMessages:List[ChatMessageContent] = history[(messageCount + 1):]

        # Save the new messages to MongoDB
        for message in newMessages:
            await db_manager.messages_collection.insert_one(
                AssistantMessageContent(
                    thread_id=run.thread_id,
                    role=message.role,
                    items=message.items
                ).to_bson()
            )

run_service = RunService()

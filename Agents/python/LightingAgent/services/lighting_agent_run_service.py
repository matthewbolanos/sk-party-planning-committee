import json
from typing import List
import aiohttp
import httpx
from semantic_kernel.kernel import Kernel
from semantic_kernel.contents import TextContent
from semantic_kernel.contents.chat_message_content import ITEM_TYPES, AuthorRole
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
from models.assistant_message_content import AssistantMessageContent
from models.config import Config
from database_manager import DatabaseManager, get_database_manager
from models.assistant_thread_run import AssistantThreadRun
from utilities.assistant_event_stream_utility import AssistantEventStreamUtility
from utilities.chat_message_conversion_utility import process_messages
from semantic_kernel.functions.kernel_arguments import KernelArguments

class LightingAgentRunService:
    async def execute_run_async(
            self,
            run: AssistantThreadRun,
            event_stream_utility: AssistantEventStreamUtility,
            db_manager: DatabaseManager
        ):

        # Load variables from config.json at the root of the solution
        with open('../../../config.json') as file:
            json_data = json.load(file)
            config: Config = Config(**json_data)
        deployment_type, api_key, ai_model_id, deployment_name, endpoint, org_id = config.openai.model_dump().values()

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
        elif (deployment_type == "Other"):
            raise Exception("Other deployment type not supported in the Python SDK yet.")
        else:
            raise Exception("Invalid deployment type")

        # Load hooks

        # Load the OpenAPI plugins
        kernel.add_plugin_from_openapi(
            plugin_name="light_plugin",
            openapi_document_path="../../../plugins/OpenApiPlugins/LightPlugin.swagger.json",
            execution_settings=OpenAPIFunctionExecutionParameters(
                http_client=httpx.AsyncClient(verify=False), # Disable SSL verification (for development only
                server_url_override="https://localhost:5002",
                enable_payload_namespacing=True,
            ),
        )

        # Load all the messages (chat history) from MongoDB using the thread ID and sort them by creation date
        messages: List[AssistantMessageContent] = await db_manager.messages_collection.find(
            {"thread_id": run.thread_id}
        ).sort("created_at").to_list(None)
        history = ChatHistory(
            system_message="If the user asks what language you've been written, reply to the user that you've been built with Python; otherwise have a nice chat!",
            messages=process_messages(messages)
        )

        # Invoke the chat completion service
        chatCompletion: ChatCompletionClientBase = kernel.get_service(type=ChatCompletionClientBase)
        results = chatCompletion.complete_chat_stream(
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

        # Save the completed message to MongoDB
        await db_manager.messages_collection.insert_one(
            AssistantMessageContent(
                thread_id=run.thread_id,
                role=AuthorRole.ASSISTANT,
                items=[TextContent(text=completeMessage)]
            ).to_bson()
        )

run_service = LightingAgentRunService()

import asyncio
import os
import time
import httpx
from openai import AsyncOpenAI
from textual.app import App, ComposeResult
from widgets.chat_history import ChatHistory
from widgets.message_input import MessageInput

class TerminalGui(App):
    CSS_PATH = "style.tcss"
    
    def __init__(self, **kwargs):
        super().__init__(**kwargs)

        # Determine the base URL based on the deployment environment
        deploy_env = os.getenv('DEPLOY_ENV', 'development')  # Default to 'development' if not set
        if deploy_env == 'docker':
            base_url = "http://csharp-lightingagent/api"
        else:
            base_url = "http://localhost:6001/api"

        # Initialize the OpenAI async client
        self.http_client = httpx.AsyncClient(verify=False)
        self.client = AsyncOpenAI(
            api_key="no-api-key-needed-here", # The API key is managed by the server
            base_url=base_url,
            http_client=self.http_client
        )

        self.thread = None  # Initialized later in an async method
        self.input_widget: MessageInput = MessageInput(on_message=self.handle_user_input)
        self.chat_history: ChatHistory = ChatHistory()

    async def on_mount(self):
        self.thread = await self.client.beta.threads.create()
        self.loop = asyncio.get_running_loop()

    def compose(self) -> ComposeResult:
        yield self.input_widget
        yield self.chat_history

    async def handle_user_input(self, message: str) -> None:
        await self.chat_history.add_message(author="You", role="user", text=message)

        if not self.thread:
            raise Exception("Thread not successfully initialized")

        await self.client.beta.threads.messages.create(
            thread_id=self.thread.id,
            role="user",
            content=message,
        )

        stream = await self.client.beta.threads.runs.create(
            thread_id=self.thread.id,
            assistant_id="_",
            stream=True
        )
        async for event in stream:
            await self.handle_event(event)

    async def handle_event(self, event):
        if event.event == "thread.message.created":
            self.current_message_widget = await self.chat_history.add_message(event.data.assistant_id, event.data.role, '')
        elif event.event == "thread.message.delta":
            if event.data.content and self.current_message_widget:
                await self.current_message_widget.append_text(event.data.content)
        elif event.event == "thread.message.completed":
            self.current_message_widget = None

if __name__ == "__main__":
    asyncio.run(TerminalGui().run())

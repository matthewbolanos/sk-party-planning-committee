import asyncio
import json
import os
import httpx
from openai import AsyncOpenAI
from textual import on
from textual.app import App, ComposeResult
from textual.widgets import OptionList, Rule, Markdown
from textual.widgets.option_list import Option
from textual.containers import Horizontal, Vertical
from services.health_check_service import HealthCheckService
from config.config import Config
from widgets.chat_history import ChatHistory
from widgets.message_input import MessageInput
import webbrowser

class TerminalGui(App):
    CSS_PATH = "style.tcss"

    agent_services = None
    health_check_service: HealthCheckService = None
    
    def __init__(self, **kwargs):
        super().__init__(**kwargs)

        # Initialize the HTTP async client
        self.http_client = httpx.AsyncClient(verify=False, follow_redirects=True)
        self.health_check_service = HealthCheckService(self.http_client)

        # Load variables from config.json at the root of the solution
        with open('../config.json') as file:
            json_data = json.load(file)
            config: Config = Config(**json_data)
        self.agent_services = config.agent_services

        # Set the client to Python by default with asyncio
        asyncio.run(self.set_client("python"))

        self.thread = None  # Initialized later in an async method
        self.input_widget: MessageInput = MessageInput(on_message=self.handle_user_input)
        self.chat_history: ChatHistory = ChatHistory()
        self.option_list = OptionList(
            Option("Python", id="python"),
            Option("C#", id="csharp"),
            Option("Java", id="java"),
            id="languages"
        )

        # Get the index of the option with the id "csharp" and highlight it
        self.option_list.highlighted = self.option_list.get_option_index("python")

    @on(OptionList.OptionHighlighted, "#languages")  
    async def change_language(self, event: OptionList.OptionMessage):
        await self.set_client(event.option_id)

    @on(Markdown.LinkClicked, "Markdown")  
    def go_to_link(self, event: Markdown.LinkClicked):
        # Open the link in the browser
        webbrowser.open(event.href)

    async def set_client(self, option_id: str):
        if (option_id == "python"):
            base_url = await self.health_check_service.get_healthy_endpoint(self.agent_services['python']['LightingAgent'].endpoints) + "/api"
        elif (option_id == "csharp"):
            base_url = await self.health_check_service.get_healthy_endpoint(self.agent_services['csharp']['LightingAgent'].endpoints) + "/api"
        elif (option_id == "java"):
            base_url = await self.health_check_service.get_healthy_endpoint(self.agent_services['java']['LightingAgent'].endpoints, "/actuator/health") + "/api"

        self.client = AsyncOpenAI(
            api_key="no-api-key-needed-here", # The API key is managed by the server
            base_url=base_url,
            http_client=self.http_client
        )

    async def on_mount(self):
        self.thread = await self.client.beta.threads.create()
        self.loop = asyncio.get_running_loop()

    def compose(self) -> ComposeResult:
        yield Horizontal(
            Vertical(
                self.option_list,
                classes="nav_pane"
            ),
            Rule(orientation="vertical", line_style="heavy"),
            Vertical(
                self.chat_history,
                self.input_widget,
                classes="chat_pane"
            ),
            classes="body"
        )

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

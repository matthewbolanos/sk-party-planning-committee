import asyncio
import os
import httpx
from openai import AsyncOpenAI
from textual import on
from textual.app import App, ComposeResult
from textual.widgets import OptionList, Rule
from textual.widgets.option_list import Option
from textual.containers import Horizontal, Vertical
from widgets.chat_history import ChatHistory
from widgets.message_input import MessageInput

class TerminalGui(App):
    CSS_PATH = "style.tcss"
    
    def __init__(self, **kwargs):
        super().__init__(**kwargs)

        # Initialize the HTTP async client
        self.http_client = httpx.AsyncClient(verify=False, follow_redirects=True)
        self.set_client("python")

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
    def change_language(self, event: OptionList.OptionMessage):
        self.set_client(event.option_id)

    def set_client(self, option_id: str):
        deploy_env = os.getenv('DEPLOY_ENV', 'development')
        if (option_id == "python"):
            if deploy_env == 'docker':
                base_url = "http://python-lightingagent/api"
            else:
                base_url = "http://localhost:7001/api"
        elif (option_id == "csharp"):
            if deploy_env == 'docker':
                base_url = "http://csharp-lightingagent/api"
            else:
                base_url = "http://localhost:7101/api"

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

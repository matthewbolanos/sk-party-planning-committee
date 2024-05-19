from typing import List
import asyncio
from textual.app import ComposeResult
from textual.containers import Container
from textual.widgets import Static, Markdown
from textual.reactive import reactive, Reactive
from openai.types.beta.threads.message_content import MessageContent

class ChatMessage(Static):
    update_count: Reactive[int] = reactive(0)
    update_interval: float = 0.15  # Interval in seconds to batch updates

    def __init__(self, author: str, role: str, text: str, **kwargs):
        super().__init__(**kwargs)
        self.text = text
        self.border_title = author
        
        # Styling based on role
        self.classes = 'user-message' if role == "user" else 'agent-message'
        
        # Initialize Markdown widget with initial text
        self.markdown = Markdown(text)
        self.markdown.border_title = self.border_title
        
        # Buffer to accumulate incoming text updates
        self.update_buffer = []
        self.update_task = None

    async def update_markdown(self):
        await self.markdown.update(self.text)

    def compose(self) -> ComposeResult:
        children = [self.markdown]
        message_container = Container(*children)
        yield message_container

    async def process_updates(self):
        while True:
            await asyncio.sleep(self.update_interval)
            if self.update_buffer:
                buffered_text = ''.join(self.update_buffer)
                self.update_buffer.clear()
                self.text += buffered_text
                self.update_count += 1
                await self.update_markdown()
                self.scroll_visible()

    async def append_text(self, text: List[MessageContent] | str):
        # Check if text is a list of MessageContent objects or a string
        if isinstance(text, list):
            for content in text:
                if content['type'] == "text":
                    self.update_buffer.append(content['text']['value'])
        else:
            self.update_buffer.append(text)

        if self.update_task is None or self.update_task.done():
            self.update_task = asyncio.create_task(self.process_updates())


import logging
from textual.app import App, ComposeResult, RenderResult
from textual.containers import Container
from textual.widget import Widget
from textual.widgets import Static, Markdown
from textual.reactive import reactive, Reactive

class ChatMessage(Static):
    update_count: Reactive[int] = reactive(0)

    def __init__(self, author: str, role: str, text: str, **kwargs):
        super().__init__(**kwargs)
        self.text = text
        self.border_title = author
        
        # Styling based on role
        self.classes = 'user-message' if role == "user" else 'agent-message'
        
        # Initialize Markdown widget with initial text
        self.markdown = Markdown(text)
        self.markdown.border_title = self.border_title

    async def update_markdown(self):
        # if len(self.text) < self.size.width:
        #     self.markdown.styles.width = len(self.text) + 4
        # else:
        #     self.markdown.styles.width = None
        await self.markdown.update(self.text)
        # await self.refresh()

    def compose(self) -> ComposeResult:
        children = [self.markdown]
        message_container = Container(*children)
        yield message_container

    async def append_text(self, text: str):
        # Append text to the reactive variable directly
        self.text += text  # Append to the string in reactive_text
        self.update_count = self.update_count + 1
        await self.update_markdown()
        self.scroll_visible()

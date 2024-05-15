from typing import List
from textual.app import ComposeResult
from textual.containers import Container
from textual.widgets import Static, Markdown
from textual.reactive import reactive, Reactive
from openai.types.beta.threads.message_content import MessageContent

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
        # if (self.update_count == 1):
        #     await self.recompose()

        # await self.refresh()

    def compose(self) -> ComposeResult:
        children = [self.markdown]
        message_container = Container(*children)
        # if len(self.text) > 0:
        yield message_container

    async def append_text(self, text: List[MessageContent] | str):
        # Check if text is a list of MessageContent objects or a string
        if isinstance(text, list):
            for content in text:
                if content['type'] == "text":
                    self.text += content['text']['value']
        else:
            self.text += text

        self.update_count = self.update_count + 1
        await self.update_markdown()
        self.scroll_visible()

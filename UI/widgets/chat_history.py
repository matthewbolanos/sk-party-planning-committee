import random
from textual.app import ComposeResult
from textual.widget import Widget
from textual.containers import VerticalScroll
from textual.reactive import reactive, Reactive

from widgets.chat_message import ChatMessage

class ChatHistory(Widget):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        test = ChatMessage(
            author="Assistant",
            role="assistant",
            text="Hello! How can I help you today?"
        )
        self.list_view =  VerticalScroll(test)

    def compose(self) -> ComposeResult:
        yield self.list_view
    
    async def add_message(self, author: str, role: str, text: str) -> ChatMessage:
        chat_message = ChatMessage(
            author=author,
            role=role,
            text=text
        )

        await self.list_view.mount(chat_message)
        chat_message.scroll_visible()

        return chat_message

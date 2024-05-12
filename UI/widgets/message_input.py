from textual.app import ComposeResult
from textual.widget import Widget
from textual.widgets import Input

class MessageInput(Widget):
    def __init__(self, on_message=None, **kwargs):
        super().__init__(**kwargs)
        self.on_message = on_message
        self.input_field = Input(placeholder="Your message here...")

    def compose(self) -> ComposeResult:
        yield self.input_field

    async def handle_input(self, message: str):
        # Optionally, clear the input field after sending the message
        self.input_field.value = ""
        # Call an external function or method if provided
        if self.on_message:
            await self.on_message(message)

    async def on_key(self, event):
        # Handle key press events, looking specifically for Enter
        if event.key == "enter":
            await self.handle_input(self.input_field.value)

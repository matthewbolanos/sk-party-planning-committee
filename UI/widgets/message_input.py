from textual.app import ComposeResult
from textual.widgets import Static, Input

class MessageInput(Static):

    def __init__(self, **kwargs):
        super().__init__(**kwargs)

    def compose(self) -> ComposeResult:
        yield Input(placeholder="First Name")
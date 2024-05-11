import logging
from textual.app import ComposeResult, RenderResult
from textual.containers import Container
from textual.widgets import Static, Markdown



class ChatMessage(Static):

    def __init__(self, text: str, author: str, type: str, **kwargs):
        super().__init__(**kwargs)
        self.text = text
        self.border_title = author
        self.type = type

        if type == "user":
            self.classes = "user-message"
        elif type == "other":
            self.classes = "agent-message"

        self.markdown = Markdown(self.text)
        self.markdown.border_title = self.border_title

    def render(self) -> RenderResult:
        # Check if message fits on one line
        logging.debug(f"Rendering message {self.text}.")
        logging.debug(f"Message length: {len(self.text)}")
        logging.debug(f"Message width: {self.markdown.size.width}")
        if len(self.text) < self.markdown.size.width:
            self.markdown.styles.width = len(self.text) + 4

        if len(self.text) > self.markdown.size.width:
            # set the width to null
            self.markdown.styles.width = None

        return super().render()


    def compose(self) -> ComposeResult:
        children = [self.markdown]

        message_container = Container(*children)

        yield message_container

        
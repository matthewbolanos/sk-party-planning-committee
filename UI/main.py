import asyncio
import logging
from textual.app import App, ComposeResult
from textual.containers import ScrollableContainer
from widgets.chat_message import ChatMessage

from widgets.message_input import MessageInput

logging.basicConfig(filename='app.log', level=logging.DEBUG)

class TerminalGui(App):
    CSS_PATH = "style.tcss"

    def compose(self) -> ComposeResult:
        yield MessageInput()
        yield ScrollableContainer(

            ChatMessage(
                "Integer posuere erat a ante venenatis dapibus posuere velit aliquet. Fusce dapibus, tellus ac cursus commodo, tortor mauris condimentum nibh, ut fermentum massa justo sit amet risus.",
                author="User",
                type="user"
            ),
            ChatMessage(
                "Cum sociis natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Lorem ipsum dolor sit amet, consectetur adipiscing elit.",
                author="Assistant",
                type="other"
            ),
            ChatMessage(
                "Integer posuere.",
                author="User",
                type="user"
            )
        )

if __name__ == "__main__":
    asyncio.run(TerminalGui().run())

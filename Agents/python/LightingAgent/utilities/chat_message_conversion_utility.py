

from typing import List
from semantic_kernel.contents import TextContent
from semantic_kernel.contents.chat_history import ChatMessageContent

def process_messages(messages: List[dict]) -> List[ChatMessageContent]:
    """Process a list of message documents into a list of ChatMessageContent."""
    chat_messages = []
    for message in messages:
        items = [
            TextContent(text=item["text"]["value"])
            for item in message["content"] if item["type"] == "text"
        ]
        chat_message_content = ChatMessageContent(
            role=message["role"],
            items=items,
        )
        chat_messages.append(chat_message_content)
    return chat_messages
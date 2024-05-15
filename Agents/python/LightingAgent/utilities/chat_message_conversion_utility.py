

from typing import List
from semantic_kernel.contents import TextContent
from semantic_kernel.contents.chat_history import ChatMessageContent
from semantic_kernel.contents import ChatMessageContent, TextContent, FunctionCallContent, FunctionResultContent

def process_messages(messages: List[dict]) -> List[ChatMessageContent]:
    """Process a list of message documents into a list of ChatMessageContent."""
    chat_messages = []
    for message in messages:
        items = []
        for item in message["content"]:
            item_type = item["type"]
            if item_type == "text":
                items.append(TextContent.from_bson(item))
            elif item_type == "functionCall":
                items.append(FunctionCallContent.from_bson(item))
            elif item_type == "functionResult":
                items.append(FunctionResultContent.from_bson(item))
            else:
                raise ValueError(f"Unknown content type: {item_type}")
        
        chat_message_content = ChatMessageContent(
            role=message["role"],
            items=items,
        )
        chat_messages.append(chat_message_content)
    return chat_messages
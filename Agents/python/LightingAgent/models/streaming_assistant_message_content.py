from pydantic import BaseModel
from typing import List, Any
from semantic_kernel.contents.streaming_chat_message_content import StreamingChatMessageContent

class StreamingAssistantMessageContentDeltaContentText(BaseModel):
    value: str
    annotations: List[Any]

class StreamingAssistantMessageContentDeltaContent(BaseModel):
    index: int
    type: str
    text: StreamingAssistantMessageContentDeltaContentText

class StreamingAssistantMessageContent(BaseModel):
    content: List[StreamingAssistantMessageContentDeltaContent]

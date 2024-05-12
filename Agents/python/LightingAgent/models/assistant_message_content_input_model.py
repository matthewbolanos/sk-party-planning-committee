from typing import List
from pydantic import BaseModel
from semantic_kernel.contents import AuthorRole
from semantic_kernel.contents.chat_message_content import ITEM_TYPES

from models.assistant_message_content import serialize_kernel_content

class AssistantMessageContentInputModel(BaseModel):
    role: AuthorRole
    content: List[ITEM_TYPES]

    class Config:
        json_encoders = {
            ITEM_TYPES: lambda v: serialize_kernel_content(v)
        }
        

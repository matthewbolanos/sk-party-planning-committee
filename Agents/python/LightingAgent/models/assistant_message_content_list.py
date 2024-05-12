from pydantic import BaseModel, Field, validator
from typing import Any, Optional
from datetime import datetime
from bson import ObjectId

# Assume imports from your semantic kernel are available
from semantic_kernel.contents.chat_message_content import ITEM_TYPES
from models.assistant_message_content_output_model import AssistantMessageContentOutputModel
from models.assistant_message_content import serialize_kernel_content

class AssistantMessageContentList(BaseModel):
    object: str = "list"
    data: list[AssistantMessageContentOutputModel]
    first_id: str
    last_id: str
    has_more: bool

from typing import List
from pydantic import BaseModel
from models.assistant_message_content_input_model import AssistantMessageContentInputModel

class AssistantThreadInputModel(BaseModel):
    messages: List[AssistantMessageContentInputModel] = []
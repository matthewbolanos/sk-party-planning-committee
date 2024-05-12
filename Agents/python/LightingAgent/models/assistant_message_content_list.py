from pydantic import BaseModel
from models.assistant_message_content_output_model import AssistantMessageContentOutputModel

class AssistantMessageContentList(BaseModel):
    object: str = "list"
    data: list[AssistantMessageContentOutputModel]
    first_id: str
    last_id: str
    has_more: bool

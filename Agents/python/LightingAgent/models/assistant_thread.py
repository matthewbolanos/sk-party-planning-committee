from typing import List
from datetime import datetime
from bson import ObjectId
from models.assistant_message_content import AssistantMessageContent
from models.assistant_thread_base import AssistantThreadBase

class AssistantThread(AssistantThreadBase):
    _messages: List[AssistantMessageContent] = []

    class Config:
        json_encoders = {
            datetime: lambda v: int(v.timestamp()),  # Converts datetime to Unix timestamp for JSON output
            ObjectId: lambda v: str(v)  # Convert ObjectId to string for JSON output
        }
        populate_by_name = True
        from_attributes = True

    def to_bson(self) -> dict:
        """Convert to BSON document for MongoDB insertion, including embedded messages."""
        document = super().to_bson()  # Get the base document
        return document

    @classmethod
    def from_bson(cls, document) -> 'AssistantThread':
        """Convert from BSON document to Pydantic model, including embedded messages."""
        instance = super().from_bson(document)
        return instance

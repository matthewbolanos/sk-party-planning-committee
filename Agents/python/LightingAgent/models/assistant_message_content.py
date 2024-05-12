from pydantic import Field, validator
from typing import Optional
from datetime import datetime
from bson import ObjectId

# Assume imports from your semantic kernel are available
from semantic_kernel.contents import ChatMessageContent, TextContent
from semantic_kernel.contents.chat_message_content import ITEM_TYPES

class AssistantMessageContent(ChatMessageContent):
    id: Optional[str] = Field(default_factory=lambda: str(ObjectId()), alias="_id")
    thread_id: Optional[str] = None
    run_id: Optional[str] = None
    assistant_id: Optional[str] = None
    created_at: datetime = Field(default_factory=lambda: datetime.utcnow())

    class Config:
        json_encoders = {
            ITEM_TYPES: lambda v: serialize_kernel_content(v),
            datetime: lambda v: int(v.timestamp()),  # Converts datetime to Unix timestamp for JSON output
            ObjectId: lambda v: str(v)  # Convert ObjectId to string for JSON output
        }
        # Allow use of MongoDB's '_id' field name
        allow_population_by_field_name = True

    @validator('created_at', pre=True, always=True)
    def default_datetime(cls, v):
        return v or datetime.utcnow()

    def to_bson(self):
        """Convert to BSON document for MongoDB insertion, using Pydantic's dict method with by_alias=True to handle field aliases."""
        document = self.dict(by_alias=True, exclude_none=True)
        document.pop("metadata")
        if "id" in document:
            document["_id"] = document.pop("id")
        # if isinstance(self.items, TextContent):
        for item in document["items"]:
            item.pop("metadata")
            if item.get("text", False):
                text = item.pop("text")
                item["type"] = "text"
                item["text"] = {"value": text, "annotations": []}
        document["content"] = document.pop("items")
        return document

    @classmethod
    def from_bson(cls, document):
        """Convert from BSON document to Pydantic model, adjusting field names and types as necessary."""
        document['id'] = str(document.pop('_id'))
        return cls(**document)


def serialize_kernel_content(content):
    if isinstance(content, TextContent):
        return {
            "type": "text",
            "text": {"value": content.text, "annotations": []}
        }
    else:
        raise TypeError("Unexpected type of content")

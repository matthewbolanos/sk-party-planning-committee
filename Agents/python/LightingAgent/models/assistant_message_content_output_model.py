from pydantic import BaseModel, Field, validator
from typing import Any, Optional
from datetime import datetime
from bson import ObjectId

# Assume imports from your semantic kernel are available
from semantic_kernel.contents import AuthorRole
from semantic_kernel.contents.chat_message_content import ITEM_TYPES
from models.assistant_message_content import serialize_kernel_content

class AssistantMessageContentOutputModel(BaseModel):
    id: Optional[str] = None
    object: str = "thread.message"
    role: AuthorRole = None
    thread_id: Optional[str] = None
    run_id: Optional[str] = None
    assistant_id: Optional[str] = None
    created_at: datetime = None
    content: Optional[list[ITEM_TYPES]] = None

    class Config():
        json_encoders = {
            ITEM_TYPES: lambda v: serialize_kernel_content(v),
            datetime: lambda v: int(v.timestamp()) if v is not None else None,
            ObjectId: lambda v: str(v),  # Convert ObjectId to string for JSON output
            AuthorRole: lambda v: AuthorRole(v).value
        }
        populate_by_name = True
    

    def to_bson(self):
        """Convert to BSON document for MongoDB insertion, using Pydantic's dict method with by_alias=True to handle field aliases."""
        document = self.model_dump(by_alias=True, exclude_none=True)
        if "id" in document:
            document["_id"] = document.pop("id")
        document["_id"] = ObjectId(document["_id"])
        for item in document["content"]:
            if item.get("text", False):
                text = item.pop("text")
                item["type"] = "text"
                item["text"] = {"value": text, "annotations": []}
        return document

    @classmethod
    def from_bson(cls, document):
        """Convert from BSON document to Pydantic model, adjusting field names and types as necessary."""
        document['id'] = str(document.pop('_id'))
        document['role'] = AuthorRole(document.pop('role'))
        document['object'] = "thread.message"
        for item in document["content"]:
            if item.get("type", False) == "text":
                text = item.pop("text")
                item["text"] = text["value"]
        return cls(**document)

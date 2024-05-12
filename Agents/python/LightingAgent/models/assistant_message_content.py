from pydantic import Field, validator
from typing import Any, Optional
from datetime import datetime
from bson import ObjectId
from semantic_kernel.contents import ChatMessageContent, TextContent
from semantic_kernel.contents.chat_message_content import ITEM_TYPES
from semantic_kernel.contents.kernel_content import KernelContent

class AssistantMessageContent(ChatMessageContent):
    id: Optional[str] = Field(default_factory=lambda: str(ObjectId()), alias="_id")
    thread_id: Optional[str] = None
    run_id: Optional[str] = None
    assistant_id: Optional[str] = None
    created_at: datetime = Field(default_factory=lambda: datetime.utcnow())
    inner_content: Optional[str] = Field(exclude=True, default=None)
    ai_model_id: Optional[str] = Field(exclude=True, default=None)
    # content: str = Field(default=None, alias="_content")
    # items: Optional[list[KernelContent]] = Field(default=None, alias="content")
    metadata: Optional[dict] = Field(exclude=True, default=None)
    name: Optional[dict] = Field(exclude=True, default=None)
    encoding: Optional[dict] = Field(exclude=True, default=None)
    finish_reason: Optional[dict] = Field(exclude=True, default=None)

    def model_dump(self, **kwargs) -> dict[str, Any]:
        # Use the super() function to get the standard dictionary representation
        d = super().model_dump(**kwargs)
        # Custom handling for content and items
        d['content'] =  d.pop('items', None)
        return d
    
    def model_dump_json(self, **kwargs) -> str:
        # Use the super() function to get the standard JSON representation
        d = super().model_dump_json(**kwargs)
        
        # Search for "items" key in json string and replace with "contents"
        d = d.replace('"items":', '"contents":')
        
        return d

    class Config():
        json_encoders = {
            KernelContent: lambda v: serialize_kernel_content(v),
            datetime: lambda v: int(v.timestamp()),  # Converts datetime to Unix timestamp for JSON output
            ObjectId: lambda v: str(v)  # Convert ObjectId to string for JSON output
        }
        # Allow use of MongoDB's '_id' field name
        populate_by_name = True
        

    # @validator('created_at', pre=True, always=True)
    # def default_datetime(cls, v):
    #     return v or datetime.utcnow()

    def to_bson(self) -> dict:
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
    def from_bson(cls, document) -> 'AssistantMessageContent':
        """Convert from BSON document to Pydantic model, adjusting field names and types as necessary."""
        document['id'] = str(document.pop('_id'))
        return cls(**document)


def serialize_kernel_content(content: ITEM_TYPES) -> ITEM_TYPES:
    if isinstance(content, TextContent):
        return {
            "type": "text",
            "text": {"value": content.text, "annotations": []}
        }
    else:
        raise TypeError("Unexpected type of content")

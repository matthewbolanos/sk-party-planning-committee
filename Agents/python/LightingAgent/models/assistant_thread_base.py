from pydantic import BaseModel, Field
from typing import Optional
from datetime import datetime
from bson import ObjectId

class ToolResources(BaseModel):
    code_interpreter: Optional[bool] = None
    file_search: Optional[bool] = None

class AssistantThreadBase(BaseModel):
    id: Optional[str] = Field(default_factory=lambda: str(ObjectId()))
    object: str = Field(default="thread", alias="object")
    created_at: datetime = Field(default_factory=lambda: datetime.utcnow(), alias="created_at")
    tool_resources: ToolResources = ToolResources()

    class Config:
        json_encoders = {
            datetime: lambda v: int(v.timestamp()),  # Converts datetime to Unix timestamp for JSON output
            ObjectId: lambda v: str(v)  # Convert ObjectId to string for JSON output
        }
        populate_by_name = True
        from_attributes = True

    def to_bson(self) -> dict:
        """Convert to BSON document for MongoDB insertion, using Pydantic's dict method with by_alias=True to handle field aliases."""
        document = self.model_dump(by_alias=True, exclude_none=True, exclude={"tool_resources"})
        if "id" in document:
            document["_id"] = document.pop("id")
        document["_id"] = ObjectId(document["_id"])
        return document

    @classmethod
    def from_bson(cls, document) -> 'AssistantThreadBase':
        """Convert from BSON document to Pydantic model, adjusting field names and types as necessary."""
        document['id'] = str(document.pop('_id'))
        return cls(**document)

from pydantic import BaseModel, Field, validator
from datetime import datetime
from uuid import uuid4
from typing import Optional

class AssistantThreadRun(BaseModel):
    id: str = Field(default_factory=lambda: str(uuid4()))
    thread_id: Optional[str] = None
    assistant_id: Optional[str] = None
    model: Optional[str] = None
    stream: bool = True
    created_at: datetime = Field(default_factory=datetime.utcnow)

    class Config:
        json_encoders = {
            datetime: lambda v: int(v.timestamp())  # Converts datetime to Unix timestamp for JSON output
        }

    @validator('created_at', pre=True, always=True)
    def default_datetime(cls, v):
        return v or datetime.utcnow()

    def to_json(self):
        """Convert to JSON, custom handling for Python's datetime and optional fields."""
        return {
            "id": self.id,
            "threadId": self.thread_id,
            "assistant_id": self.assistant_id,
            "model": self.model,
            "stream": self.stream,
            "created_at": int(self.created_at.timestamp())
        }

    @classmethod
    def from_json(cls, data):
        """Construct the object from a JSON-like dictionary."""
        data['created_at'] = datetime.utcfromtimestamp(data['created_at'])
        return cls(**data)


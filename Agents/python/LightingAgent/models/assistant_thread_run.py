from pydantic import BaseModel, Field
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

    # @validator('created_at', pre=True, always=True)
    # def default_datetime(cls, v):
    #     return v or datetime.utcnow()


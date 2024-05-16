from typing import Dict, List, Optional
from pydantic import BaseModel, Field

class AgentService(BaseModel):
    endpoints: List[str] = Field(..., alias="Endpoints")

class Config(BaseModel):
    agent_services: Dict[str, Dict[str, AgentService]] = Field(..., alias="Agents")
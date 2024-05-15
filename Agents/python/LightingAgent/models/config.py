from typing import Dict, List, Optional
from pydantic import BaseModel, Field

class OpenAIConfig(BaseModel):
    deployment_type: str = Field(..., alias="DeploymentType", required=True)
    api_key: Optional[str] = Field(None, alias="ApiKey")
    ai_model_id: Optional[str] = Field(None, alias="ModelId")
    deployment_name: Optional[str] = Field(None, alias="DeploymentName")
    endpoint: Optional[str] = Field(None, alias="Endpoint")
    org_id: Optional[str] = Field(None, alias="OrgId")

class PluginService(BaseModel):
    endpoints: List[str] = Field(..., alias="Endpoints")

class Config(BaseModel):
    openai: OpenAIConfig = Field(..., alias="OpenAI")
    plugin_services: Dict[str, PluginService] = Field(..., alias="PluginServices")
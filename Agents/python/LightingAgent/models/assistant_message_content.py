import json
from pydantic import Field
from typing import Any, Optional, List
from datetime import datetime
from bson import ObjectId
from semantic_kernel.contents.author_role import AuthorRole
from semantic_kernel.contents import ChatMessageContent, TextContent, FunctionCallContent, FunctionResultContent
from semantic_kernel.contents.chat_message_content import ITEM_TYPES
from semantic_kernel.contents.kernel_content import KernelContent
from semantic_kernel.functions.kernel_arguments import KernelArguments

class AssistantMessageContent(ChatMessageContent):
    id: Optional[str] = Field(default_factory=lambda: str(ObjectId()), alias="id")
    object: str = "thread.message"
    role: AuthorRole
    items: list[ITEM_TYPES]
    thread_id: Optional[str] = None
    run_id: Optional[str] = None
    assistant_id: Optional[str] = None
    created_at: datetime = Field(default_factory=lambda: datetime.utcnow())
    inner_content: Optional[str] = Field(exclude=True, default=None)
    ai_model_id: Optional[str] = Field(exclude=True, default=None)
    metadata: Optional[dict] = Field(exclude=True, default=None)
    name: Optional[dict] = Field(exclude=True, default=None)
    encoding: Optional[dict] = Field(exclude=True, default=None)
    finish_reason: Optional[dict] = Field(exclude=True, default=None)

    def __init__(self, **data):
        super().__init__(**data)
        self.items = data.get('items', [])

    def model_dump(self, **kwargs) -> dict[str, Any]:
        d = super().model_dump(**kwargs)
        d['content'] = d.pop('items', None)
        return d
    
    def model_dump_json(self, **kwargs) -> str:
        d = super().model_dump_json(**kwargs)
        d = d.replace('"items":', '"contents":')
        return d

    class Config:
        json_encoders = {
            KernelContent: lambda v: serialize_kernel_content(v),
            datetime: lambda v: int(v.timestamp()),  # Converts datetime to Unix timestamp for JSON output
            ObjectId: lambda v: str(v)  # Convert ObjectId to string for JSON output
        }
        # Allow use of MongoDB's '_id' field name
        populate_by_name = True

    def to_bson(self) -> dict:
        """Convert to BSON document for MongoDB insertion, using Pydantic's dict method with by_alias=True to handle field aliases."""
        document = self.model_dump(by_alias=True, exclude_none=True)
        if "id" in document:
            document["_id"] = document.pop("id")
        document["_id"] = ObjectId(document["_id"])
        if "content" in document:
            for item in document["content"]:
                item.pop("metadata")
                if "text" in item:
                    text = item.pop("text")
                    item["type"] = "text"
                    item["text"] = {"value": text, "annotations": []}
                elif "arguments" in item:
                    id = item.pop("id")
                    name = item.pop("name")
                    item.pop("index")
                    arguments = item.pop("arguments")
                    item["type"] = "functionCall"

                    # split the name into pluginName and functionName
                    plugin_name, function_name = name.split("-")

                    item["functionCall"] = {
                        "pluginName": plugin_name,
                        "functionName": function_name,
                        "id": id,
                        "arguments": arguments # TODO: fix this
                    }
                elif "result" in item:
                    id = item.pop("id")
                    name = item.pop("name")
                    result = item.pop("result")
                    item["type"] = "functionResult"

                    # split the name into pluginName and functionName
                    plugin_name, function_name = name.split("-")

                    item["functionResult"] = {
                        "pluginName": plugin_name,
                        "functionName": function_name,
                        "id": id,
                        "result": result
                    }
        return document

    @classmethod
    
    def from_bson(cls, document) -> 'AssistantMessageContent':
        """Convert from BSON document to Pydantic model, adjusting field names and types as necessary."""
        document['id'] = str(document.pop('_id'))
        assistantMessageContent = {
            "id": document["id"],
            "object": "thread.message",
            "items": [],
            "role": document["role"],
            "thread_id": document["thread_id"] if "thread_id" in document else None,
            "run_id": document["run_id"] if "run_id" in document else None,
            "assistant_id": document["assistant_id"] if "assistant_id" in document else None
        }

        if "content" in document:
            for item in document["content"]:
                item_type = item["type"]
                if item_type == "text":
                    assistantMessageContent["items"].append(TextContent(text=item["text"]["value"]))
                elif item_type == "functionCall":
                    # arguments = item["functionCall"]["arguments"]
                    # deserialize_arguments = json.loads(arguments)
                    # kernel_arguments = KernelArguments(**deserialize_arguments)

                    assistantMessageContent["items"].append(FunctionCallContent(
                        name=item["functionCall"]["pluginName"] + "-" + item["functionCall"]["functionName"],
                        id=item["functionCall"]["id"],
                        arguments=item["functionCall"]["arguments"]
                    ))
                elif item_type == "functionResult":
                    assistantMessageContent["items"].append(FunctionResultContent(
                        name=item["functionResult"]["pluginName"] + "-" + item["functionResult"]["functionName"],
                        id=item["functionResult"]["id"],
                        result=item["functionResult"]["result"]
                    ))
        return cls(**assistantMessageContent)


def serialize_kernel_content(content: ITEM_TYPES) -> ITEM_TYPES:
    if isinstance(content, TextContent):
        return {
            "type": "text",
            "text": {"value": content.text, "annotations": []}
        }
    elif isinstance(content, FunctionCallContent):
        return {
            "type": "functionCall",
            "functionCall": {
                "name": content.name,
                "id": content.id,
                "arguments": json.dumps(content.arguments)
            }
        }
    elif isinstance(content, FunctionResultContent):
        return {
            "type": "functionResult",
            "functionResult": {
                "name": content.name,
                "id": content.id,
                "result": content.result
            }
        }
    else:
        raise TypeError("Unexpected type of content")

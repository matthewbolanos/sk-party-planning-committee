from datetime import datetime
from fastapi import APIRouter, HTTPException, Body, Depends, Query
from typing import List, Optional
from models.assistant_message_content_list import AssistantMessageContentList
from models.assistant_message_content_output_model import AssistantMessageContentOutputModel
from models.assistant_message_content import AssistantMessageContent
from models.assistant_message_content_input_model import AssistantMessageContentInputModel
from database_manager import DatabaseManager, get_database_manager
from semantic_kernel.contents import AuthorRole, TextContent
from bson import ObjectId

message_router = APIRouter()


@message_router.post("/{thread_id}/messages/", response_model=AssistantMessageContentOutputModel, status_code=201)
async def create_message(thread_id: str, message_input: AssistantMessageContentInputModel = Body(...), db_manager: DatabaseManager = Depends(get_database_manager)):
    if not message_input:
        raise HTTPException(status_code=400, detail="Message input is required.")

    thread = await db_manager.threads_collection.find_one({"_id": ObjectId(thread_id)})
    if not thread:
        raise HTTPException(status_code=404, detail=f"Thread with ID '{thread_id}' not found.")

    # Check if message_input.content is a string or array
    if isinstance(message_input.content[0].text, str):
        message_input.content = [TextContent(text=message_input.content[0].text)]

    new_message = AssistantMessageContent(
        thread_id=thread_id,
        role=AuthorRole(message_input.role),
        content='_'
    )
    new_message.items = message_input.content
    await db_manager.messages_collection.insert_one(new_message.to_bson())

    output = AssistantMessageContentOutputModel(
        id=new_message.id,
        thread_id=new_message.thread_id,
        run_id=new_message.run_id,
        assistant_id=new_message.assistant_id,
        created_at=new_message.created_at,
        content=message_input.content
    )

    return output

@message_router.get("/{thread_id}/messages/{message_id}/", response_model=AssistantMessageContentOutputModel)
async def retrieve_message(thread_id: str, message_id: str, db_manager = Depends(get_database_manager)):
    message = await db_manager.messages_collection.find_one({"_id": ObjectId(message_id), "thread_id": thread_id})
    if not message:
        raise HTTPException(status_code=404, detail=f"Message with ID '{message_id}' not found in thread '{thread_id}'.")
    return AssistantMessageContentOutputModel.from_bson(message)

@message_router.get("/{thread_id}/messages/", response_model=AssistantMessageContentList)
async def list_messages(
        thread_id: str, 
        limit: int = Query(20, gt=0), 
        order: str = Query("desc", regex="^(asc|desc)$"), 
        after: Optional[str] = None, 
        before: Optional[str] = None, 
        db_manager = Depends(get_database_manager)):
    filters = {"thread_id": thread_id}

    sort = 1 if order == "asc" else -1
    options = {"sort": [("created_at", sort)], "limit": min(limit, 100)}

    cursor_filter = {}

    if after:
        cursor_filter["created_at"] = {"$gt": datetime.datetime.fromtimestamp(int(after))}
    elif before:
        cursor_filter["created_at"] = {"$lt": datetime.datetime.fromtimestamp(int(before))}

    query_filter = {"$and": [filters, cursor_filter]} if cursor_filter else filters

    messages = await db_manager.messages_collection.find(query_filter).to_list(None)

    # Convert BSON documents to Pydantic models
    messages = [AssistantMessageContentOutputModel.from_bson(message) for message in messages]

    result = {
        "object": "list",
        "data": messages,
        "first_id": messages[0].id if messages else None,
        "last_id": messages[-1].id if messages else None,
        "has_more": len(messages) == limit
    }

    return result

@message_router.delete("/{thread_id}/messages/{message_id}/", status_code=204)
async def delete_message(thread_id: str, message_id: str, db_manager = Depends(get_database_manager)):
    result = await db_manager.messages_collection.delete_one({"_id": ObjectId(message_id), "thread_id": thread_id})
    if result.deleted_count == 0:
        raise HTTPException(status_code=404, detail=f"Message with ID '{message_id}' not found in thread '{thread_id}'.")
    return {"detail": "Message deleted successfully"}

@message_router.put("/{thread_id}/messages/{message_id}/", response_model=AssistantMessageContent)
async def update_message(thread_id: str, message_id: str, message_update: AssistantMessageContentInputModel = Body(...), db_manager = Depends(get_database_manager)):
    raise HTTPException(status_code=501, detail="Not implemented")
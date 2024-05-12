from datetime import datetime
from fastapi import APIRouter, HTTPException, Body, Depends, Query
from typing import List, Optional
from models.assistant_message_content import AssistantMessageContent
from models.assistant_message_content_input_model import AssistantMessageContentInputModel
from database_manager import DatabaseManager, get_database_manager
from bson import ObjectId

message_router = APIRouter()


@message_router.post("/{thread_id}/messages/", response_model=AssistantMessageContent, status_code=201)
async def create_message(thread_id: str, message_input: AssistantMessageContentInputModel = Body(...), db_manager: DatabaseManager = Depends(get_database_manager)):
    if not message_input:
        raise HTTPException(status_code=400, detail="Message input is required.")

    thread = await db_manager.threads_collection.find_one({"_id": ObjectId(thread_id)})
    if not thread:
        raise HTTPException(status_code=404, detail=f"Thread with ID '{thread_id}' not found.")

    new_message = AssistantMessageContent(**message_input.dict(), thread_id=thread_id, created_at=datetime.utcnow())
    await db_manager.messages_collection.insert_one(new_message.dict(by_alias=True))
    return new_message

@message_router.get("/{thread_id}/messages/{message_id}/", response_model=AssistantMessageContent)
async def retrieve_message(thread_id: str, message_id: str, db_manager = Depends(get_database_manager)):
    message = await db_manager.messages_collection.find_one({"_id": ObjectId(message_id), "thread_id": ObjectId(thread_id)})
    if not message:
        raise HTTPException(status_code=404, detail=f"Message with ID '{message_id}' not found in thread '{thread_id}'.")
    return message

@message_router.get("/{thread_id}/messages/", response_model=List[AssistantMessageContent])
async def list_messages(thread_id: str, limit: int = Query(20, gt=0), order: str = Query("desc", regex="^(asc|desc)$"), after: Optional[str] = None, before: Optional[str] = None, db_manager = Depends(get_database_manager)):
    query = {"thread_id": ObjectId(thread_id)}
    sort_order = 1 if order == "asc" else -1
    options = {"sort": [("created_at", sort_order)], "limit": min(limit, 100)}

    if after:
        query["created_at"] = {"$gt": ObjectId(after)}
    elif before:
        query["created_at"] = {"$lt": ObjectId(before)}

    messages = await db_manager.messages_collection.find(query, options).to_list(None)
    return messages

@message_router.delete("/{thread_id}/messages/{message_id}/", status_code=204)
async def delete_message(thread_id: str, message_id: str, db_manager = Depends(get_database_manager)):
    result = await db_manager.messages_collection.delete_one({"_id": ObjectId(message_id), "thread_id": ObjectId(thread_id)})
    if result.deleted_count == 0:
        raise HTTPException(status_code=404, detail=f"Message with ID '{message_id}' not found in thread '{thread_id}'.")
    return {"detail": "Message deleted successfully"}

@message_router.put("/{thread_id}/messages/{message_id}/", response_model=AssistantMessageContent)
async def update_message(thread_id: str, message_id: str, message_update: AssistantMessageContentInputModel = Body(...), db_manager = Depends(get_database_manager)):
    raise HTTPException(status_code=501, detail="Not implemented")
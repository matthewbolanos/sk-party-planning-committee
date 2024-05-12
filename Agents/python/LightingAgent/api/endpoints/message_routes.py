from datetime import datetime
from fastapi import APIRouter, HTTPException, Body, Depends, Query, status
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
    # Check if message_input is empty
    if not message_input:
        raise HTTPException(status_code=400, detail="Message input is required.")

    # Check if thread exists
    thread = await db_manager.threads_collection.find_one({"_id": ObjectId(thread_id)})
    if not thread:
        raise HTTPException(status_code=404, detail=f"Thread with ID '{thread_id}' not found.")

    # Check if message_input.content is a string or array
    if isinstance(message_input.content[0].text, str):
        message_input.content = [TextContent(text=message_input.content[0].text)]

    # Create a new message object to save in the database

    # Because the content field in ChatMessageContent is a string when we need it to be a list
    # of KernelContent objects populated by the items field, we need to do some clever manipulation
    #   1) Create a new message object with the content field set to a placeholder string
    #   2) Set the items field to the content field
    #   3) Within the AssistantMessageContent class, the model_dump and model_dump_json methods
    #      will convert the items field to the content field when the object is serialized

    new_message = AssistantMessageContent(
        thread_id=thread_id,
        role=AuthorRole(message_input.role),
        content='_' # Step 1
    )
    new_message.items = message_input.content # Step 2
    await db_manager.messages_collection.insert_one(new_message.to_bson())

    # Create a new message object to return in the response
    return AssistantMessageContentOutputModel(
        id=new_message.id,
        thread_id=new_message.thread_id,
        role=new_message.role,
        run_id=new_message.run_id,
        assistant_id=new_message.assistant_id,
        created_at=new_message.created_at,
        content=message_input.content
    )

@message_router.get("/{thread_id}/messages/{message_id}/", response_model=AssistantMessageContentOutputModel)
async def retrieve_message(thread_id: str, message_id: str, db_manager: DatabaseManager = Depends(get_database_manager)):
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
        db_manager: DatabaseManager = Depends(get_database_manager)):
    
    # Set up the query filter and sort order
    filters = {"thread_id": thread_id}

    # Set the sort order based on the query parameter
    sort = 1 if order == "asc" else -1
    options = {"sort": [("created_at", sort)], "limit": min(limit, 100)}

    # Set up the cursor filter based on the after and before query parameters
    cursor_filter = {}
    if after:
        cursor_filter["created_at"] = {"$gt": datetime.datetime.fromtimestamp(int(after))}
    elif before:
        cursor_filter["created_at"] = {"$lt": datetime.datetime.fromtimestamp(int(before))}

    # Combine the filters and cursor filter
    query_filter = {"$and": [filters, cursor_filter]} if cursor_filter else filters

    # Retrieve the messages from the database using filter, sort, and options
    messages = await db_manager.messages_collection.find(query_filter, **options).to_list(limit)

    # Convert BSON documents to Pydantic models
    messages = [AssistantMessageContentOutputModel.from_bson(message) for message in messages]

    return {
        "object": "list",
        "data": messages,
        "first_id": messages[0].id if messages else None,
        "last_id": messages[-1].id if messages else None,
        "has_more": len(messages) == limit
    }

@message_router.delete("/{thread_id}/messages/{message_id}/", status_code=status.HTTP_200_OK)
async def delete_thread(thread_id: str, message_id: str, db_manager: DatabaseManager = Depends(get_database_manager)):
    # Delete the thread from the database
    result = await db_manager.messages_collection.delete_one({"_id": ObjectId(thread_id), "thread_id": thread_id})
    if result.deleted_count:
        return {"id": message_id, "object": "message.deleted", "deleted": True}
    
    # If the message was not found, raise an exception
    raise HTTPException(status_code=404, detail={
        "id": message_id,
        "object": "message.deleted",
        "deleted": False
    })

@message_router.put("/{thread_id}/messages/{message_id}/", response_model=AssistantMessageContent)
async def update_message(thread_id: str, message_id: str, message_update: AssistantMessageContentInputModel = Body(...), db_manager = Depends(get_database_manager)):
    raise HTTPException(status_code=501, detail="Not implemented")
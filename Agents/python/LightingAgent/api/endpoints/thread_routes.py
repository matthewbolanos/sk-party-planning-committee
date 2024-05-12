import json
from fastapi import APIRouter, HTTPException, status, Depends
from typing import List
from models.assistant_thread import AssistantThread
from models.assistant_thread_input_model import AssistantThreadInputModel
from models.assistant_message_content import AssistantMessageContent
from database_manager import DatabaseManager, get_database_manager
from semantic_kernel.contents import AuthorRole, TextContent
from semantic_kernel.contents.chat_message_content import ITEM_TYPES
from bson import ObjectId

thread_router = APIRouter()

@thread_router.post("/", response_model=AssistantThread, status_code=status.HTTP_201_CREATED)
async def create_thread(thread_input: AssistantThreadInputModel, db_manager:DatabaseManager = Depends(get_database_manager)):
    # Create a new thread object
    new_thread = AssistantThread()

    # Convert the message content to the appropriate format
    messages: list[AssistantMessageContent] = []
    for message in thread_input.messages:

        # Create a new list to store the items
        items: list[ITEM_TYPES] = []
        for item in message.content:

            # Check if the class is a text item (this is the only supported type for now)
            if isinstance(item, TextContent):
                items.append(TextContent(
                    text=item.text
                ))

        # Create a new message content object with the items
        new_message=AssistantMessageContent(
            thread_id=new_thread.id,
            role=AuthorRole(message.role),
            items=items
        )
        messages.append(new_message)

    # Set the messages for the new thread
    new_thread._messages = messages
    
    # Insert the new thread and messages into the database
    inserted_thread = await db_manager.threads_collection.insert_one(new_thread.to_bson())
    if inserted_thread.inserted_id:
        if messages:
            await db_manager.messages_collection.insert_many([message.to_bson() for message in messages])
        return new_thread
    
    # If the thread was not inserted, raise an exception
    raise HTTPException(status_code=400, detail="Failed to create thread")

@thread_router.get("/{thread_id}", response_model=AssistantThread)
async def get_thread(thread_id: str, db_manager: DatabaseManager = Depends(get_database_manager)):
    # Find the thread in the database
    thread = await db_manager.threads_collection.find_one({"_id": ObjectId(thread_id)})
    if thread:
        return AssistantThread.from_bson(thread)
    
    # If the thread was not found, raise an exception
    raise HTTPException(status_code=404, detail="Thread not found")

@thread_router.delete("/{thread_id}", status_code=status.HTTP_200_OK)
async def delete_thread(thread_id: str, db_manager: DatabaseManager = Depends(get_database_manager)):
    # Delete the thread from the database
    result = await db_manager.threads_collection.delete_one({"_id": ObjectId(thread_id)})
    if result.deleted_count:
        # Delete the messages associated with the thread
        await db_manager.messages_collection.delete_many({"thread_id": thread_id})
        return {"id": thread_id, "object": "thread.deleted", "deleted": True}
    
    # If the thread was not found, raise an exception
    raise HTTPException(status_code=404, detail={
        "id": thread_id,
        "object": "thread.deleted",
        "deleted": False
    })

@thread_router.put("/{thread_id}", response_model=AssistantThread)
async def update_thread(thread_id: str, thread_update: AssistantThreadInputModel, db_manager: DatabaseManager = Depends(get_database_manager)):
    raise HTTPException(status_code=501, detail="Not implemented")
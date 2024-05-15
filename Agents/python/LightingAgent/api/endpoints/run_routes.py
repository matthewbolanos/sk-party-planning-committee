from datetime import datetime
import os
from fastapi import APIRouter, HTTPException, Depends, Response
from pydantic import BaseModel
from utilities.assistant_event_stream_utility import AssistantEventStreamService
from models.assistant_message_content import AssistantMessageContent
from models.assistant_thread_run import AssistantThreadRun
from services.lighting_agent_run_service import LightingAgentRunService
from database_manager import DatabaseManager, get_database_manager
from starlette.responses import StreamingResponse
from semantic_kernel.contents import AuthorRole, TextContent
from bson import ObjectId
from pymongo.errors import ConnectionFailure

run_router = APIRouter()
from fastapi import APIRouter, Depends, HTTPException, Response
from starlette.responses import StreamingResponse

run_router = APIRouter()

@run_router.post("/{thread_id}/runs/", status_code=201)
async def create_run(
    thread_id: str, 
    response: Response, 
    db_manager: DatabaseManager = Depends(get_database_manager)  # Dependency is injected here
):
    thread = await db_manager.threads_collection.find_one({"_id": ObjectId(thread_id)})
    if not thread:
        raise HTTPException(status_code=404, detail=f"Thread with ID '{thread_id}' not found.")
    
    new_run = AssistantThreadRun(thread_id=thread_id, created_at=datetime.utcnow())
    streamingUtility = AssistantEventStreamService()
    run_service = LightingAgentRunService()  # No need to pass db_manager to constructor

    async def create_event_stream(run: AssistantThreadRun):
        db_manager = DatabaseManager(os.getenv('MONGODB_URL'))
        await db_manager.connect()  # Explicitly managing connection
        try:
            yield streamingUtility.create_event("thread.run.created", run)
            async for event in run_service.execute_run_async(run, streamingUtility, db_manager):
                yield event  # Each event generated here
            yield streamingUtility.create_done_event()

        except HTTPException as e:
            # Handle specific HTTP exceptions if necessary
            yield f"Error: {str(e.detail)}"
        
        except ConnectionFailure as e:
            # Handle MongoDB connection issues
            yield "Database connection error occurred."

        finally:
            await db_manager.disconnect() 

    return StreamingResponse(create_event_stream(new_run), headers={"Content-Type": "text/event-stream"})

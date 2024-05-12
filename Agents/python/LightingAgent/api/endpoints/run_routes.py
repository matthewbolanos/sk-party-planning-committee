from datetime import datetime
from fastapi import APIRouter, HTTPException, Depends, Response
from pydantic import BaseModel
from utilities.assistant_event_stream_utility import AssistantEventStreamUtility
from models.assistant_message_content import AssistantMessageContent
from models.assistant_thread_run import AssistantThreadRun
from services.lighting_agent_run_service import run_service
from database_manager import DatabaseManager, get_database_manager
from starlette.responses import StreamingResponse
from semantic_kernel.contents import AuthorRole, TextContent
from bson import ObjectId

run_router = APIRouter()

@run_router.post("/{thread_id}/runs/", status_code=201)
async def create_run(thread_id: str, response: Response, db_manager: DatabaseManager = Depends(get_database_manager)):
    thread = await db_manager.threads_collection.find_one({"_id": ObjectId(thread_id)})
    if not thread:
        raise HTTPException(status_code=404, detail=f"Thread with ID '{thread_id}' not found.")

    new_run = AssistantThreadRun(
        thread_id=thread_id,
        created_at=datetime.utcnow()
    )
    streamingUtility: AssistantEventStreamUtility = AssistantEventStreamUtility()
    
    return StreamingResponse(create_event_stream(new_run, streamingUtility))

def create_event_stream(run: AssistantThreadRun, assistantEventStreamUtility: AssistantEventStreamUtility):
    yield assistantEventStreamUtility.create_event("thread.run.created", run)
    yield assistantEventStreamUtility.create_event("thread.run.queued", run)
    yield assistantEventStreamUtility.create_event("thread.run.in_progress", run)
    yield assistantEventStreamUtility.create_event("thread.run.step.created", run)
    yield assistantEventStreamUtility.create_event("thread.run.step.in_progress", run)
    
    # run service
    for event in run_service.execute_run_async(run, assistantEventStreamUtility):
        yield event

    yield assistantEventStreamUtility.create_event("thread.run.completed", run)
    yield assistantEventStreamUtility.create_event("thread.run.step.completed", run)
    yield assistantEventStreamUtility.create_done_event()



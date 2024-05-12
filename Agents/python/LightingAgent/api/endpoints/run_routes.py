from datetime import datetime
from fastapi import APIRouter, HTTPException, Depends, Response
from models.assistant_thread_run import AssistantThreadRun
from services.lighting_agent_run_service import run_service
from database_manager import DatabaseManager, get_database_manager
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
    # Simulate run creation and handling
    response.headers['Content-Type'] = 'text/event-stream'
    return run_service.create_event_stream(new_run)

def create_event_stream(run: AssistantThreadRun):
    async def event_stream():
        async for event in run_service.execute_run_async(run):
            yield event
    return event_stream()

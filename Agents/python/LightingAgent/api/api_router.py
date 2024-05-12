from fastapi import APIRouter
from api.endpoints.thread_routes import thread_router
from api.endpoints.message_routes import message_router
from api.endpoints.run_routes import run_router

api_router = APIRouter()

# Include various endpoint routers
api_router.include_router(thread_router, prefix="/api/threads", tags=["threads"])
api_router.include_router(message_router, prefix="/api/threads", tags=["messages"])
api_router.include_router(run_router, prefix="/api/threads", tags=["runs"])


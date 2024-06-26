from contextlib import asynccontextmanager
from fastapi import FastAPI
from api.api_router import api_router

def create_application() -> FastAPI:
    application = FastAPI(
        title='Lighting Agent API',
        version='1.0.0',
        description='API for managing home automation devices and interactions.',
    )

    @application.get("/health")
    async def health_check():
        return {"status": "up"}
    
    # Add API routers
    application.include_router(api_router)

    return application

app = create_application()

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8001, log_level="info")

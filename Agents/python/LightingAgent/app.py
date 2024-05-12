from fastapi import FastAPI
from motor.motor_asyncio import AsyncIOMotorClient, AsyncIOMotorDatabase

app = FastAPI()

# Configuration for MongoDB
DATABASE_URL = "mongodb://localhost:27017"
DATABASE_NAME = "your_database_name"

@app.on_event("startup")
async def startup_db_client():
    app.mongodb_client = AsyncIOMotorClient(DATABASE_URL)
    app.mongodb = app.mongodb_client[DATABASE_NAME]

@app.on_event("shutdown")
async def shutdown_db_client():
    app.mongodb_client.close()

# Example route
@app.get("/")
async def root():
    return {"message": "Hello World"}

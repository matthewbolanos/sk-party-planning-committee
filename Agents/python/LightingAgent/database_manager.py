from fastapi import Depends, HTTPException
from motor.motor_asyncio import AsyncIOMotorClient

class DatabaseManager:
    client: AsyncIOMotorClient = None

    def __init__(self, url: str):
        self.url = url
        self.db = None
        self.threads_collection = None
        self.messages_collection = None

    async def connect(self):
        self.client = AsyncIOMotorClient(self.url)
        self.db = self.client['PartyPlanning']
        self.threads_collection = self.db['Threads']
        self.messages_collection = self.db['Messages']

    async def disconnect(self):
        if self.client:
            self.client.close()

# Dependency
async def get_database_manager():
    db_manager = DatabaseManager("mongodb://localhost:27017")
    await db_manager.connect()
    try:
        yield db_manager
    finally:
        await db_manager.disconnect()
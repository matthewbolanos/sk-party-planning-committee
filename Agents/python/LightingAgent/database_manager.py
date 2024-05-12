import os
from fastapi import Depends, HTTPException
from motor.motor_asyncio import AsyncIOMotorClient, AsyncIOMotorDatabase, AsyncIOMotorCollection

class DatabaseManager:
    client: AsyncIOMotorClient = None
    db: AsyncIOMotorDatabase = None
    threads_collection: AsyncIOMotorCollection = None
    messages_collection: AsyncIOMotorCollection = None

    def __init__(self, url: str):
        self.url = url

    async def connect(self):
        self.client = AsyncIOMotorClient(self.url)
        self.db = self.client['PartyPlanning']
        self.threads_collection = self.db['Threads']
        self.messages_collection = self.db['Messages']

    async def disconnect(self):
        if self.client:
            self.client.close()

async def get_database_manager():
    db_manager = DatabaseManager(os.getenv('MONGODB_URL'))
    await db_manager.connect()
    try:
        yield db_manager
    finally:
        await db_manager.disconnect()

from flask import Flask, jsonify
import aiohttp
import asyncio

app = Flask(__name__)

class HealthCheckService:
    def __init__(self):
        self.session = aiohttp.ClientSession()

    async def get_healthy_endpoint(self, endpoints, health_check_path="/health"):
        for endpoint in endpoints:
            if await self.is_endpoint_healthy(endpoint, health_check_path):
                return endpoint
        raise Exception("All endpoints are down.")

    async def is_endpoint_healthy(self, endpoint, health_check_path="/health"):
        try:
            async with self.session.get(f"{endpoint}{health_check_path}") as response:
                return response.status == 200
        except:
            return False

    async def close(self):
        await self.session.close()
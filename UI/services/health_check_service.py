from httpx import AsyncClient

class HealthCheckService:
    client: AsyncClient

    def __init__(self, client: AsyncClient):
        self.client = client

    async def get_healthy_endpoint(self, endpoints, health_check_path="/health"):
        for endpoint in endpoints:
            if await self.is_endpoint_healthy(endpoint, health_check_path):
                return endpoint
        raise Exception("All endpoints are down: " + str(endpoints))

    async def is_endpoint_healthy(self, endpoint, health_check_path="/health"):
        try:
            response = await self.client.get(f"{endpoint}{health_check_path}")
            return response.status_code == 200
        except:
            return False

    async def close(self):
        await self.client.aclose()
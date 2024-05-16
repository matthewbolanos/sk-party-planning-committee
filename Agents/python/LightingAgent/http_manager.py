
import httpx

class HttpManager:
    client: httpx.AsyncClient = httpx.AsyncClient(verify=False, follow_redirects=True)
    
    def __init__(self):
        pass

    async def disconnect(self):
        if self.client:
            self.client.close()

async def get_http_client():
    manager = HttpManager()
    return manager.client

import httpx

class HttpManager:
    client: httpx.AsyncClient = httpx.AsyncClient(
        verify=False,
        follow_redirects=True,
        timeout= httpx.Timeout(
            connect=1.0,  # seconds to wait for a connection to be established
            read=60.0,    # seconds to wait for data to be read
            write=10.0,   # seconds to wait for data to be written
            pool=20.0     # seconds to wait for a connection to become available from the pool
        )
    )
    
    def __init__(self):
        pass

    async def disconnect(self):
        if self.client:
            self.client.close()

async def get_http_client():
    manager = HttpManager()
    return manager.client
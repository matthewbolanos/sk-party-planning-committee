import json
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field
from typing import List, Optional, Sequence
import httpx
import os
from weaviate import Client as WeaviateClient, AuthClientPassword
from semantic_kernel.connectors.memory.weaviate import weaviate_memory_store
from semantic_kernel.connectors.ai.open_ai import OpenAITextEmbedding

from config import Config

app = FastAPI(
    title="Scene API",
    version="v1",
    description="Generates a light color palette for a scene based on a description; must use when setting the color of lights."
)


embeddingService = OpenAITextEmbedding(ai_model_id="text-embedding-ada-002")

# Try to connect to Weaviate with the provided endpoints until one works
weaviate_client = WeaviateClient(
    url="http://localhost:8080",
)

class SceneRequest(BaseModel):
    threeWordDescription: str = Field(..., min_length=1, description="The palette to generate in 1-3 sentence (feel free to be creative!)")
    recommendedColors: str = Field(..., min_length=1, description="The name of 3 recommended colors for the scene based on your expertise (no need to ask the user; just do it!)")

class ScenePallette(BaseModel):
    imageUrl: Optional[str] = None
    colors: Optional[List[str]] = None

async def generate_image(prompt: str) -> str:
    headers = {
        "Authorization": f"Bearer {api_key}",
        "Content-Type": "application/json"
    }
    data = {
        "prompt": prompt,
        "n": 1,
        "size": "256x256"
    }
    timeout = httpx.Timeout(120.0)  
    async with httpx.AsyncClient(timeout=timeout) as client:
        response = await client.post("https://api.openai.com/v1/images/generations", headers=headers, json=data)
        response.raise_for_status()
        image_url = response.json()["data"][0]["url"]
    return image_url

async def download_image(url: str) -> str:
    async with httpx.AsyncClient() as client:
        response = await client.get(url)
        response.raise_for_status()
        image_bytes = response.content
        output_path = os.path.join("/tmp", "image.png")
        with open(output_path, "wb") as image_file:
            image_file.write(image_bytes)
    return output_path

def get_top_colors(image_path: str, color_count: int) -> List[str]:
    from colorthief import ColorThief
    color_thief = ColorThief(image_path)
    palette = color_thief.get_palette(color_count=color_count)
    return [f'#{r:02x}{g:02x}{b:02x}' for r, g, b in palette]


# OpenAI and Weaviate configurations
with open('../../config.json') as file:
    json_data = json.load(file)
    config: Config = Config(**json_data)
deployment_type, api_key, ai_model_id, deployment_name, endpoint, org_id = config.openai.model_dump().values()
endpoints = config.weaviate.model_dump().values()


@app.post("/Scene", response_model=ScenePallette, tags=["Scene"], summary="Generates a light color palette for a scene based on a description; _must_ use when setting the color of lights.")
async def generate_scene_pallette(scene_request: SceneRequest):
    complete_prompt = f"{scene_request.threeWordDescription}{'; ' + scene_request.recommendedColors if scene_request.recommendedColors else ''}"

    try:
        embedding = await embeddingService.generate_embeddings(complete_prompt)
        result = weaviate_client.query.get("scene", ["sk_text", "sk_description", "sk_additional_metadata"]).with_near_vector({'vector': embedding}).with_limit(1).do()
        if result['data']['Get']['Scene']:
            return ScenePallette(
                imageUrl=result['data']['Get']['Scene'][0]['sk_additional_metadata'],
                colors=json.loads(result['data']['Get']['Scene'][0]['sk_description'])
            )
    except Exception as e:
        pass

    # Generate an image
    image_url = await generate_image(f"Realistic image for desktop background: {complete_prompt}")
    local_image_path = await download_image(image_url)
    hex_colors = get_top_colors(local_image_path, 5)

    # Cache the scene
    weaviate_client.data_object.create({
        "sk_additional_metadata": image_url,
        "sk_description": json.dumps(hex_colors),
        "sk_text": scene_request.threeWordDescription,
    }, "scene", vector=embedding)

    return ScenePallette(imageUrl=image_url, colors=hex_colors)

@app.get("/health")
async def health_check():
    return {"status": "up"}

# To run the app, use the command: uvicorn app:app --reload

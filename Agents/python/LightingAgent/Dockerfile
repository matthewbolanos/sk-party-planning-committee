# Dockerfile
FROM python:3.12

WORKDIR /app

COPY Agents/python/LightingAgent/requirements.txt ./
RUN pip install --no-cache-dir -r requirements.txt

COPY config.json .
COPY ./PluginResources ./PluginResources
COPY Agents/python/LightingAgent ./Agents/python/LightingAgent

# Change working directory to the app directory
WORKDIR /app/Agents/python/LightingAgent
CMD [ "uvicorn", "app:app", "--reload", "--host", "0.0.0.0", "--port", "80" ]
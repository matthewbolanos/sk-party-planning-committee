# Dockerfile
FROM python:3.12

WORKDIR /app

COPY UI/requirements.txt ./
COPY config.json ./
RUN pip install --no-cache-dir -r requirements.txt

COPY ./UI ./UI

WORKDIR /app/UI

CMD [ "python", "./main.py" ]

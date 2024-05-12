# in services/run_service.py
import asyncio
from models.assistant_thread_run import AssistantThreadRun
from utilities.assistant_event_stream_utility import AssistantEventStreamUtility

class LightingAgentRunService:
    def execute_run_async(self, run: AssistantThreadRun, event_stream_utility: AssistantEventStreamUtility):
        # Simulating an event stream for a thread run
        events = [
            "Im.so.excited",
        ]
        for event in events:
            yield event_stream_utility.create_event(event, run)

run_service = LightingAgentRunService()

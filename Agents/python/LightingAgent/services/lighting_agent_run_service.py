# in services/run_service.py
import asyncio
from models.assistant_thread_run import AssistantThreadRun

class LightingAgentRunService:
    async def execute_run_async(self, run: AssistantThreadRun):
        # Simulating an event stream for a thread run
        events = [
            "thread.run.created",
            "thread.run.queued",
            "thread.run.in_progress",
            "thread.run.step.created",
            "thread.run.step.in_progress",
            "thread.run.completed",
            "thread.run.step.completed",
            "thread.run.done"
        ]
        for event in events:
            yield f"data: thread.run.created\n\n"
            await asyncio.sleep(1)  # simulate delay for each step

run_service = LightingAgentRunService()

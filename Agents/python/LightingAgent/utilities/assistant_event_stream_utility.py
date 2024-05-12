
import json
from typing import Optional
from pydantic import BaseModel
from models.assistant_message_content import AssistantMessageContent
from dataclasses import dataclass
from datetime import datetime
from typing import Any, Generator, Optional
from openai.types.chat.chat_completion_chunk import ChatCompletionChunk
from semantic_kernel.contents.streaming_chat_message_content import StreamingChatMessageContent
from semantic_kernel.contents import TextContent, AuthorRole


@dataclass
class StreamingChatCompletionsUpdate:
    Id: str
    FinishReason: Optional[str]

class AssistantEventStreamUtility:
    _current_message: Optional[AssistantMessageContent] = None

    def create_message_event(self, run_id: str, data: StreamingChatMessageContent) -> Generator[str, None, None]:
        streaming_chat_completions_update: ChatCompletionChunk = data.InnerContent

        if (self._current_message is not None and streaming_chat_completions_update.id != self._current_message.id):
            yield self.create_error_event("Previous message was not finished.")
            return

        if self._current_message is None:
            self._current_message = AssistantMessageContent(
                id=streaming_chat_completions_update.id,
                thread_id=self._current_thread_id,
                created_at=datetime.now(),
                role=AuthorRole.Assistant,
                assistant_id="LightingAgent",
                run_id=run_id,
                items=[]
            )
            yield self.create_event("thread.message.created", self._current_message)
            yield self.create_event("thread.message.in_progress", self._current_message)

        self._message_builder.append(data.content)

        yield self.create_event("thread.message.delta", data)

        if streaming_chat_completions_update.choices[0].finish_reason is not None:
            self._current_message.items = [TextContent(''.join(self._message_builder))]
            yield self.create_event("thread.message.completed", self._current_message)
            self._current_message = None

    def create_event(self, event_type: str, data: BaseModel) -> str:
        json_data = data.model_dump_json()
        return f"event: {event_type}\n" + f"data: {json_data}\n\n"

    def create_error_event(self, message: str) -> str:
        error_data = {"message": message}
        json_data = json.dumps(error_data)
        return f"event: error\n" + f"data: {json_data}\n\n"

    def create_done_event(self) -> str:
        return "event: done\n" + "data: [DONE]\n\n"

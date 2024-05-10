using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Shared.Models;

namespace LightingAgent.Controllers
{
    /// <summary>
    /// Controller for managing runs within a specific thread.
    /// </summary>
    [ApiController]
    [Route("/api/threads/{threadId}/runs")]
    public class RunController : ControllerBase
    {
        private readonly IMongoCollection<AssistantThread> _threadsCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="RunController"/> class.
        /// </summary>
        /// <param name="database">The MongoDB database.</param>
        public RunController(IMongoDatabase database)
        {
            _threadsCollection = database.GetCollection<AssistantThread>("threads");
        }

        /// <summary>
        /// Creates a new run within a specific thread.
        /// </summary>
        /// <param name="threadId">The ID of the thread to create the run in</param>
        /// <param name="newRun">The run to be created</param>
        /// <returns>Server-sent events stream of the run</returns>
        [HttpPost]
        public async Task<IActionResult> CreateRun(string threadId, [FromBody] Run newRun)
        {
            if (string.IsNullOrEmpty(threadId) || newRun == null || string.IsNullOrEmpty(newRun.AssistantId))
            {
                return BadRequest("Thread ID and assistant ID are required.");
            }

            var thread = await _threadsCollection.Find(t => t.Id == threadId).FirstOrDefaultAsync();

            if (thread == null)
            {
                return NotFound($"Thread with ID '{threadId}' not found.");
            }

            newRun.ThreadId = threadId;
            newRun.CreatedAt = DateTime.UtcNow;

            // Dummy event stream example, replace with actual run logic
            async IAsyncEnumerable<string> RunEventStream()
            {
                yield return "event: thread.run.created\n";
                yield return $"data: {{ \"id\": \"run_123\", \"object\": \"thread.run\", \"created\": {new DateTimeOffset(newRun.CreatedAt).ToUnixTimeSeconds()} }}\n\n";
                await Task.Delay(1000);

                yield return "event: thread.run.queued\n";
                yield return $"data: {{ \"id\": \"run_123\", \"object\": \"thread.run\", \"queued\": {new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()} }}\n\n";
                await Task.Delay(1000);

                yield return "event: thread.run.in_progress\n";
                yield return $"data: {{ \"id\": \"run_123\", \"object\": \"thread.run\", \"in_progress\": {new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()} }}\n\n";
                await Task.Delay(1000);

                yield return "event: thread.run.step.created\n";
                yield return $"data: {{ \"id\": \"step_001\", \"object\": \"thread.run.step\", \"created\": {new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()} }}\n\n";
                await Task.Delay(1000);

                yield return "event: thread.run.step.in_progress\n";
                yield return $"data: {{ \"id\": \"step_001\", \"object\": \"thread.run.step\", \"in_progress\": {new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()} }}\n\n";
                await Task.Delay(1000);

                yield return "event: thread.message.created\n";
                yield return $"data: {{ \"id\": \"msg_001\", \"object\": \"thread.message\", \"created\": {new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()} }}\n\n";
                await Task.Delay(1000);

                yield return "event: thread.message.in_progress\n";
                yield return $"data: {{ \"id\": \"msg_001\", \"object\": \"thread.message\", \"in_progress\": {new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()} }}\n\n";
                await Task.Delay(1000);

                yield return "event: thread.message.delta\n";
                yield return $"data: {{ \"id\": \"msg_001\", \"object\": \"thread.message.delta\", \"created\": {new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()} }}\n\n";
                await Task.Delay(1000);

                yield return "event: thread.run.completed\n";
                yield return $"data: {{ \"id\": \"run_123\", \"object\": \"thread.run\", \"completed\": {new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()} }}\n\n";
                await Task.Delay(1000);

                yield return "event: thread.run.step.completed\n";
                yield return $"data: {{ \"id\": \"step_001\", \"object\": \"thread.run.step\", \"completed\": {new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()} }}\n\n";
                await Task.Delay(1000);

                yield return "event: done\n";
                yield return "data: [DONE]\n\n";
            }

            Response.ContentType = "text/event-stream";
            await foreach (var message in RunEventStream())
            {
                await Response.WriteAsync(message);
                await Response.Body.FlushAsync();
            }

            return new EmptyResult();
        }
    }
}

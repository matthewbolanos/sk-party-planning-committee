using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Shared.Models;
using Shared.Utilities;
using LightingAgent.Services;
using Microsoft.Extensions.Options;
using Shared.Config;

namespace LightingAgent.Controllers
{
    /// <summary>
    /// Controller for managing runs within a specific thread.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="RunController"/> class.
    /// </remarks>
    /// <param name="database">The MongoDB database.</param>
    /// <param name="runService">The run service.</param>
    /// <param name="agentConfig">Discovery information for the agent</param>
    /// <param name="assistantEventStreamUtility">Provides utilities to manage Assistant API stream events</param>
    [ApiController]
    [Route("/api/threads/{threadId}/runs")]
    public class RunController(
        IMongoDatabase database,
        IRunService runService,
        IOptions<AgentConfig> agentConfig,
        AssistantEventStreamUtility assistantEventStreamUtility
    ) : ControllerBase
    {
        private readonly IMongoCollection<AssistantThreadBase> _threadsCollection = database.GetCollection<AssistantThreadBase>("Threads");
        private readonly IRunService _runService = runService;

        /// <summary>
        /// Creates a new run within a specific thread.
        /// </summary>
        /// <param name="threadId">The ID of the thread to create the run in</param>
        /// <param name="newRun">The run to be created</param>
        /// <returns>Server-sent events stream of the run</returns>
        [HttpPost]
        public async Task<IActionResult> CreateRun(string threadId)
        {
            if (string.IsNullOrEmpty(threadId))
            {
                return BadRequest("Thread ID is required.");
            }


            var thread = await _threadsCollection.Find(t => t.Id == threadId).FirstOrDefaultAsync();

            if (thread == null)
            {
                return NotFound($"Thread with ID '{threadId}' not found.");
            }

            var newRun = new AssistantThreadRun()
            {
                ThreadId = threadId,
                AssistantId = agentConfig.Value.Name,
                CreatedAt = DateTime.UtcNow
            };

            async IAsyncEnumerable<string> RunEventStream()
            {
                yield return assistantEventStreamUtility.CreateEvent("thread.run.created", newRun);
                yield return assistantEventStreamUtility.CreateEvent("thread.run.queued", newRun);
                yield return assistantEventStreamUtility.CreateEvent("thread.run.in_progress", newRun);
                yield return assistantEventStreamUtility.CreateEvent("thread.run.step.created", newRun);
                yield return assistantEventStreamUtility.CreateEvent("thread.run.step.in_progress", newRun);

                await foreach (var events in _runService.ExecuteRunAsync(newRun))
                {
                    yield return events;
                }
                
                yield return assistantEventStreamUtility.CreateEvent("thread.run.completed", newRun);
                yield return assistantEventStreamUtility.CreateEvent("thread.run.step.completed", newRun);
                yield return assistantEventStreamUtility.CreateDoneEvent();
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

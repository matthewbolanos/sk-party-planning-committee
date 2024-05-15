using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PartyPlanning.Agents.Shared.Models;
using Microsoft.Extensions.Options;
using PartyPlanning.Agents.Shared.Config;
using PartyPlanning.Agents.Shared.Services;
using Microsoft.AspNetCore.Http;

namespace PartyPlanning.Agents.Shared.Controllers
{
    /// <summary>
    /// Controller for managing runs within a specific thread.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="RunController"/> class.
    /// </remarks>
    /// <param name="database">The MongoDB database.</param>
    /// <param name="runService">The run service.</param>
    /// <param name="AgentConfiguration">Discovery information for the agent</param>
    /// <param name="assistantEventStreamService">Provides utilities to manage Assistant API stream events</param>
    [ApiController]
    [Route("/api/threads/{threadId}/runs")]
    public class RunController(
        IMongoDatabase database,
        RunService runService,
        IOptions<AgentConfiguration> AgentConfiguration,
        AssistantEventStreamService assistantEventStreamService
    ) : ControllerBase
    {
        private readonly IMongoCollection<AssistantThreadBase> _threadsCollection = database.GetCollection<AssistantThreadBase>("Threads");
        private readonly RunService _runService = runService;

        /// <summary>
        /// Creates a new run within a specific thread.
        /// </summary>
        /// <param name="threadId">The ID of the thread to create the run in</param>
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
                AssistantId = AgentConfiguration.Value.Name,
                CreatedAt = DateTime.UtcNow
            };

            async IAsyncEnumerable<string> RunEventStream()
            {
                yield return assistantEventStreamService.CreateEvent("thread.run.created", newRun);
                yield return assistantEventStreamService.CreateEvent("thread.run.queued", newRun);
                yield return assistantEventStreamService.CreateEvent("thread.run.in_progress", newRun);
                yield return assistantEventStreamService.CreateEvent("thread.run.step.created", newRun);
                yield return assistantEventStreamService.CreateEvent("thread.run.step.in_progress", newRun);

                await foreach (var events in _runService.ExecuteRunAsync(newRun))
                {
                    yield return events;
                }
                
                yield return assistantEventStreamService.CreateEvent("thread.run.completed", newRun);
                yield return assistantEventStreamService.CreateEvent("thread.run.step.completed", newRun);
                yield return assistantEventStreamService.CreateDoneEvent();
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

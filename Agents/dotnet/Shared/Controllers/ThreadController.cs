using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using MongoDB.Bson;
using MongoDB.Driver;
using Shared.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Shared.Controllers
{
    /// <summary>
    /// Controller for the thread object
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ThreadController"/> class.
    /// </remarks>
    /// <param name="database">The MongoDB database.</param>
    [ApiController]
    [Route("api/threads")]
    public class ThreadController(IMongoDatabase database) : ControllerBase
    {
        private readonly IMongoCollection<AssistantThreadBase> _threadsCollection = database.GetCollection<AssistantThreadBase>("Threads");
        private readonly IMongoCollection<AssistantMessageContent> _messagesCollection = database.GetCollection<AssistantMessageContent>("Messages");

        /// <summary>
        /// Creates a new thread.
        /// </summary>
        /// <param name="input">Thread to be created</param>
        /// <response code="201">Returns the newly created thread</response>
        [HttpPost]
        [ProducesResponseType(typeof(AssistantThreadBase), 201)]
        public async Task<IActionResult> CreateThread([FromBody] ThreadInputModel input)
        {
            if (input == null)
            {
                return BadRequest("Thread is required.");
            }

            // Generate thread ID
            var threadId = ObjectId.GenerateNewId().ToString();

            // Create thread message content
            List<AssistantMessageContent> AssistantMessageContents = [];
            foreach (var message in input.Messages)
            {
                // Add the message to the thread
                AssistantMessageContents.Add(new AssistantMessageContent
                {
                    ThreadId = threadId,
                    Role = message.Role,
                    Items = [.. message.Content]
                });
            }

            var newThread = new AssistantThreadBase
            {
                Id = threadId
            };


            // Save data in parallel
            List<Task> tasks = [_threadsCollection.InsertOneAsync(newThread)];
            if (AssistantMessageContents.Count > 0)
            {
                tasks.Add(_messagesCollection.InsertManyAsync(AssistantMessageContents));
            }
            await Task.WhenAll(tasks);

            return CreatedAtRoute("RetrieveThread", new { id = threadId }, newThread);
        }

        /// <summary>
        /// Gets a thread by its ID.
        /// </summary>
        /// <param name="id">The ID of the thread to retrieve</param>
        /// <returns>The requested thread</returns>
        [HttpGet("{id}", Name = "RetrieveThread")]
        public async Task<IActionResult> RetrieveThread(string id)
        {
            var thread = await _threadsCollection.Find(t => t.Id == id).FirstOrDefaultAsync();

            if (thread == null)
            {
                return NotFound();
            }

            return Ok(thread);
        }

        /// <summary>
        /// Updates an existing thread.
        /// </summary>
        /// <param name="id">The ID of the thread to update</param>
        /// <returns>The updated thread</returns>
        [HttpPut("{id}", Name = "ModifyThread")]
        public IActionResult ModifyThread(string id)
        {
            // return that the operation is not supported
            return StatusCode(405);
        }

        /// <summary>
        /// Deletes a thread by its ID and returns confirmation.
        /// </summary>
        /// <param name="id">The ID of the thread to delete</param>
        /// <returns>Status of the deletion</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteThread(string id)
        {
            var result = await _threadsCollection.DeleteOneAsync(t => t.Id == id);

            if (result.IsAcknowledged && result.DeletedCount > 0)
            {
                return Ok(new
                {
                    id,
                    @object = "thread.deleted",
                    deleted = true
                });
            }

            return NotFound(new
            {
                id,
                @object = "thread.deleted",
                deleted = false
            });
        }
    }
}

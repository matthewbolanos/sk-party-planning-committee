using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Shared.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LightingAgent.Controllers
{
    /// <summary>
    /// Controller for the thread object
    /// </summary>
    [ApiController]
    [Route("/api/threads")]
    public class ThreadController : ControllerBase
    {
        private readonly IMongoCollection<AssistantThread> _threadsCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadController"/> class.
        /// </summary>
        /// <param name="database">The MongoDB database.</param>
        public ThreadController(IMongoDatabase database)
        {
            _threadsCollection = database.GetCollection<AssistantThread>("threads");
        }


        /// <summary>
        /// Creates a new thread.
        /// </summary>
        /// <param name="input">Thread to be created</param>
        /// <returns>The created thread</returns>
        [HttpPost("threads")]
        public async Task<IActionResult> CreateThread([FromBody] ThreadInputModel input)
        {
            if (input == null)
            {
                return BadRequest("Thread is required.");
            }

            var newThread = new AssistantThread
            {
                Messages = input.Messages
            };


            await _threadsCollection.InsertOneAsync(newThread);

            return CreatedAtRoute("RetrieveThread", new { id = newThread.Id }, newThread);
        }

        /// <summary>
        /// Gets a thread by its ID.
        /// </summary>
        /// <param name="id">The ID of the thread to retrieve</param>
        /// <returns>The requested thread</returns>
        [HttpGet("threads/{id}", Name = "RetrieveThread")]
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
        /// <param name="updatedThread">The updated thread object</param>
        /// <returns>The updated thread</returns>
        [HttpPut("threads/{id}", Name = "ModifyThread")]
        public async Task<IActionResult> ModifyThread(string id, [FromBody] AssistantThread updatedThread)
        {
            if (updatedThread == null || updatedThread.Id != id)
            {
                return BadRequest("Thread ID mismatch.");
            }

            var result = await _threadsCollection.ReplaceOneAsync(t => t.Id == id, updatedThread);

            if (result.IsAcknowledged && result.ModifiedCount > 0)
            {
                return Ok(updatedThread);
            }

            return NotFound();
        }

        /// <summary>
        /// Deletes a thread by its ID and returns confirmation.
        /// </summary>
        /// <param name="id">The ID of the thread to delete</param>
        /// <returns>Status of the deletion</returns>
        [HttpDelete("threads/{id}")]
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

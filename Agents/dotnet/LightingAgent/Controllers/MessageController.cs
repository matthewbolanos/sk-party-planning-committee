using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using MongoDB.Driver;
using Shared.Models;

namespace LightingAgent.Controllers
{
    /// <summary>
    /// Controller for the message object
    /// </summary>
    [ApiController]
    [Route("/api/threads/{threadId}/messages")]
    public class MessageController : ControllerBase
    {
        private readonly IMongoCollection<AssistantMessageContent> _messagesCollection;
        private readonly IMongoCollection<AssistantThreadBase> _threadsCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageController"/> class.
        /// </summary>
        /// <param name="database">The MongoDB database.</param>
        public MessageController(IMongoDatabase database)
        {
            _messagesCollection = database.GetCollection<AssistantMessageContent>("Messages");
            _threadsCollection = database.GetCollection<AssistantThreadBase>("Threads");
        }

        /// <summary>
        /// Creates a new message in a specific thread.
        /// </summary>
        /// <param name="threadId">The ID of the thread to create the message in</param>
        /// <param name="newMessage">The message to be created</param>
        /// <returns>The created message</returns>
        [HttpPost]
        public async Task<IActionResult> CreateMessage(string threadId, [FromBody] AssistantMessageContent newMessage)
        {
            if (string.IsNullOrEmpty(threadId) || newMessage == null)
            {
                return BadRequest("Thread ID and message are required.");
            }

            var thread = await _threadsCollection.Find(t => t.Id == threadId).FirstOrDefaultAsync();

            if (thread == null)
            {
                return NotFound($"Thread with ID '{threadId}' not found.");
            }

            newMessage.ThreadId = threadId;
            await _messagesCollection.InsertOneAsync(newMessage);

            return CreatedAtRoute("RetrieveMessage", new { threadId, id = newMessage.Id }, newMessage);
        }

        /// <summary>
        /// Retrieves a message by its ID within a specific thread.
        /// </summary>
        /// <param name="threadId">The ID of the thread to retrieve the message from</param>
        /// <param name="id">The ID of the message to retrieve</param>
        /// <returns>The requested message</returns>
        [HttpGet("{id}", Name = "RetrieveMessage")]
        public async Task<IActionResult> RetrieveMessage(string threadId, string id)
        {
            var message = await _messagesCollection.Find(m => m.ThreadId == threadId && m.Id == id).FirstOrDefaultAsync();

            if (message == null)
            {
                return NotFound($"Message with ID '{id}' not found in thread '{threadId}'.");
            }

            return Ok(message);
        }

                /// <summary>
        /// Retrieves all messages in a specific thread with optional pagination.
        /// </summary>
        /// <param name="threadId">The ID of the thread to retrieve messages from</param>
        /// <param name="limit">The maximum number of messages to return</param>
        /// <param name="order">Sorting order of messages based on creation time</param>
        /// <param name="after">Cursor to specify starting point for pagination</param>
        /// <param name="before">Cursor to specify ending point for pagination</param>
        /// <returns>A list of messages within the specified thread</returns>
        [HttpGet]
        public async Task<IActionResult> ListMessages(
            string threadId,
            [FromQuery] int limit = 20,
            [FromQuery] string order = "desc",
            [FromQuery] string? after = null,
            [FromQuery] string? before = null)
        {
            var filters = Builders<AssistantMessageContent>.Filter.Eq(m => m.ThreadId, threadId);

            var sort = order.ToLower() == "asc"
                ? Builders<AssistantMessageContent>.Sort.Ascending(m => m.CreatedAt)
                : Builders<AssistantMessageContent>.Sort.Descending(m => m.CreatedAt);

            var cursorFilter = Builders<AssistantMessageContent>.Filter.Empty;
            if (!string.IsNullOrEmpty(after))
            {
                cursorFilter = Builders<AssistantMessageContent>.Filter.Gt(m => m.CreatedAt, DateTimeOffset.FromUnixTimeSeconds(long.Parse(after)).UtcDateTime);
            }
            else if (!string.IsNullOrEmpty(before))
            {
                cursorFilter = Builders<AssistantMessageContent>.Filter.Lt(m => m.CreatedAt, DateTimeOffset.FromUnixTimeSeconds(long.Parse(before)).UtcDateTime);
            }

            var queryFilter = Builders<AssistantMessageContent>.Filter.And(filters, cursorFilter);

            var messages = await _messagesCollection
                .Find(queryFilter)
                .Sort(sort)
                .Limit(Math.Clamp(limit, 1, 100))
                .ToListAsync();

            var result = new
            {
                @object = "list",
                data = messages,
                first_id = messages.Count > 0 ? messages[0].Id : null,
                last_id = messages.Count > 0 ? messages[^1].Id : null,
                has_more = messages.Count == limit
            };

            return Ok(result);
        }

        /// <summary>
        /// Updates an existing message within a specific thread.
        /// </summary>
        /// <param name="threadId">The ID of the thread containing the message to update</param>
        /// <param name="id">The ID of the message to update</param>
        /// <returns>The updated message</returns>
        [HttpPut("{id}")]
        public IActionResult ModifyMessage(string threadId, string id)
        {
            // Return not supported
            return StatusCode(405);
        }

        /// <summary>
        /// Deletes a message by its ID and returns confirmation.
        /// </summary>
        /// <param name="threadId">The ID of the thread containing the message to delete</param>
        /// <param name="id">The ID of the message to delete</param>
        /// <returns>Status of the deletion</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(string threadId, string id)
        {
            var result = await _messagesCollection.DeleteOneAsync(m => m.ThreadId == threadId && m.Id == id);

            if (result.IsAcknowledged && result.DeletedCount > 0)
            {
                return Ok(new
                {
                    id,
                    @object = "message.deleted",
                    deleted = true
                });
            }

            return NotFound(new
            {
                id,
                @object = "message.deleted",
                deleted = false
            });
        }
    }
}

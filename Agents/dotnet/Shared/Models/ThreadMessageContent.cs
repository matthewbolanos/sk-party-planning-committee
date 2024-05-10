using System;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Shared.Converters;
using Shared.Swagger;
using Swashbuckle.AspNetCore.Annotations;

namespace Shared.Models
{
    /// <summary>
    /// Model representing a run within a thread.
    /// </summary>
    [JsonConverter(typeof(ThreadMessageContentConverter))]
    [SwaggerSchemaFilter(typeof(ThreadMessageContentSchemaFilter))]
    public class ThreadMessageContent() : ChatMessageContent
    {
        /// <summary>
        /// The ID of message.
        /// <summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        /// <summary>
        /// The ID of the thread this message belongs to.
        /// </summary>
        public string? ThreadId { get; set; }

        /// <summary>
        /// When the message was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

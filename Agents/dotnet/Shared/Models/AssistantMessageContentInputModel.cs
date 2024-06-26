using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PartyPlanning.Agents.Shared.Converters;

#pragma warning disable CS8618

namespace PartyPlanning.Agents.Shared.Models
{
    /// <summary>
    /// Represents a thread input model.
    /// </summary>
    public class AssistantMessageContentInputModel
    {
        [JsonPropertyName("role")]
        [JsonConverter(typeof(AuthorRoleConverter))]
        public AuthorRole Role { get; set; }

        [JsonPropertyName("content")]
        public List<KernelContent> Content { get; set; } = [];
    }
}
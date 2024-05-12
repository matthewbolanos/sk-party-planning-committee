#pragma warning disable CS8618

namespace Shared.Config
{
    public class OpenAIConfig
    {
        public OpenAIDeploymentType DeploymentType { get; set; }
        public string ApiKey { get; set; }
        public string ModelId { get; set; }
        public string? DeploymentName { get; set; }
        public string? Endpoint { get; set; }
        public string? OrgId { get; set; }
    }
}

public enum OpenAIDeploymentType
{
    AzureOpenAI,
    OpenAI,
    Other
}
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PartyPlanning.Agents.Shared.Config;

namespace PartyPlanning.Agents.Shared.Services
{
    public class AzureContainerAppTokenService 
    {
        private static string? cachedToken;
        private readonly ILogger<AzureContainerAppTokenService> logger;

        private string tenantId;
        private string clientId;
        private string clientSecret;

        public AzureContainerAppTokenService(ILogger<AzureContainerAppTokenService> logger, IOptions<CodeInterpreterConfiguration> codeInterpreterConfiguration)
        {
            this.logger = logger;
            this.tenantId = codeInterpreterConfiguration.Value.TenantId;
            this.clientId = codeInterpreterConfiguration.Value.ClientId;
            this.clientSecret = codeInterpreterConfiguration.Value.ClientSecret;
        }

        public async Task<string> GetTokenAsync()
        {
            if (cachedToken is null)
            {
                string resource = "https://acasessions.io/.default";

                string accessToken = await GetAccessTokenAsync(tenantId, clientId, clientSecret, resource);

                // Attempt to get the token
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Access token obtained successfully");
                }
                cachedToken = accessToken;
            }

            return cachedToken;
        } 

        private static async Task<string> GetAccessTokenAsync(string tenantId, string clientId, string clientSecret, string resource)
        {
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var tokenRequestContext = new TokenRequestContext([resource]);
            AccessToken accessToken = await credential.GetTokenAsync(tokenRequestContext);

            return accessToken.Token;
        }
    }
}
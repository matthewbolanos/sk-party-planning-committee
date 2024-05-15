using System.IO;
using Microsoft.Extensions.Configuration;

namespace PartyPlanning.Agents.Shared.Config
{
    public static class SharedConfigReader
    {
        public static IConfiguration? GetConfiguration()
        {
            // Get all the resources in the assembly
            var resources = typeof(SharedConfigReader).Assembly.GetManifestResourceNames();

            var resourceStream = typeof(SharedConfigReader).Assembly
                .GetManifestResourceStream("PartyPlanning.Agents.config.json");

            // Check if the resource stream is null
            if (resourceStream == null)
            {
                return null;
            }

            var embeddedProvider = new ConfigurationBuilder()
                .AddJsonStream(resourceStream)
                .Build();

            return embeddedProvider;
        }
    }
}

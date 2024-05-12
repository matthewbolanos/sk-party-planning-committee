using System.IO;
using Microsoft.Extensions.Configuration;

namespace Shared.Config
{
    public static class SharedConfigReader
    {
        public static IConfiguration? GetConfiguration()
        {
            var resourceStream = typeof(SharedConfigReader).Assembly
                .GetManifestResourceStream("Shared.config.json");

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

using System.IO;
using Microsoft.Extensions.Configuration;

namespace SharedConfig
{
    public static class SharedConfigReader
    {
        public static IConfiguration? GetConfiguration()
        {
            var resourceStream = typeof(SharedConfigReader).Assembly
                .GetManifestResourceStream("SharedConfig.sharedsettings.json");

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

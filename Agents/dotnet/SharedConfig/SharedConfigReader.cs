using System.IO;
using Microsoft.Extensions.Configuration;

namespace SharedConfig
{
    public static class SharedConfigReader
    {
        public static IConfiguration GetConfiguration()
        {
            var embeddedProvider = new ConfigurationBuilder()
                .AddJsonStream(typeof(SharedConfigReader).Assembly
                    .GetManifestResourceStream("SharedConfig.sharedsettings.json")!)
                .Build();

            return embeddedProvider;
        }
    }
}

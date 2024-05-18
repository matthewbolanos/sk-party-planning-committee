using Microsoft.Extensions.Options;
using PartyPlanning.PluginServices.Shared.Config;
using Q42.HueApi;

namespace PartyPlanning.PluginServices.LightService.Options
{
    public static class IHostApplicationBuilderExtensions
    {

        public static void ConfigureHueServices(this IHostApplicationBuilder builder)
        {
            builder.Services.Configure<HueBridgeOptions>(options =>
            {
                IConfigurationSection? sharedConfig = SharedConfigReader.GetConfiguration()?.GetSection("HueBridge");
                builder.Configuration.Bind("HueBridge", options);

                // If there is a shared configuration, bind it to the options
                sharedConfig?.Bind(options);
            });

            builder.Services.AddSingleton(options =>
            {
                var hueBridgeOptions = options.GetRequiredService<IOptions<HueBridgeOptions>>().Value;
                return new LocalHueClient(hueBridgeOptions.IpAddress, hueBridgeOptions.ApiKey);
            });
        }
    }
}
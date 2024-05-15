#pragma warning disable CS8618

namespace PartyPlanning.Agents.Shared.Config
{
    public class PluginServicesConfiguration : Dictionary<string, PluginServiceConfiguration>
    {
    }

    public class PluginServiceConfiguration
    {
        public List<string> Endpoints { get; set; } = new();
    }
}


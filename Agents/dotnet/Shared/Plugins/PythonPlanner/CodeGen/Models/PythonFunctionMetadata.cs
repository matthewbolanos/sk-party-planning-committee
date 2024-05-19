namespace PartyPlanning.Agents.Shared.Plugins.PythonPlanner.CodeGen.Models;

public class PythonFunctionMetadata(string pluginName, string name, PythonParameterMetadata arguments, PythonParameterMetadata? returns = null, string? description = null)
{
    public string PluginName { get; set; } = pluginName;
    public string Name { get; set; } = name;
    public PythonParameterMetadata Arguments { get; set; } = arguments;
    public PythonParameterMetadata? Returns { get; set; } = returns;
    public bool IsRequired {
        get {
            return Arguments.Properties.Any(p => p.IsRequired);
        }
    }
    public string? Description { get; set; } = description;
}
namespace PartyPlanning.Agents.Shared.Plugins.PythonPlanner.CodeGen.Models;

public class PythonPluginGeneratorSettings
{
    public bool IsMock { get; set; }

    public FunctionFilters FunctionFilters { get; set; } = new FunctionFilters();
}
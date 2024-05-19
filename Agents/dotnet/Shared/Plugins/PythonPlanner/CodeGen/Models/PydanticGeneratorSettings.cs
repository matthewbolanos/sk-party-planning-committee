namespace PartyPlanning.Agents.Shared.Plugins.PythonPlanner.CodeGen.Models;

public class PythonPluginGeneratorSettings
{
    public bool AddToExistingFunctionClass { get; set; } = false;

    public FunctionFilters FunctionFilters { get; set; } = new FunctionFilters();
}
using Microsoft.SemanticKernel;
using PartyPlanning.Agents.Shared.Plugins.PythonPlanner.CodeGen.Models;

namespace PartyPlanning.Agents.Shared.Plugins.PythonPlanner.CodeGen.Extension;

internal static class KernelPluginCollectionExtensions
{
    public static KernelPluginCollection GetFunctions(this KernelPluginCollection pluginCollection, FunctionFilters? filters = null)
    {
        filters ??= new FunctionFilters();

        return new(pluginCollection
            .Where(p => filters.ShouldIncludePlugin(p.Name))
            .Select(p => KernelPluginFactory.CreateFromFunctions(
                p.Name,
                p.Where(f => filters.ShouldIncludeFunction(new FunctionName(p.Name,f.Name)))
            )));
    }
}
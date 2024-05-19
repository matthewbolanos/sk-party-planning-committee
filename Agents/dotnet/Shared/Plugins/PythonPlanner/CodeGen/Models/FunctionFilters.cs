namespace PartyPlanning.Agents.Shared.Plugins.PythonPlanner.CodeGen.Models
{
    public class FunctionFilters
    {
        private List<FunctionName>? _includedFunctions;
        private List<FunctionName>? _excludedFunctions;

        public List<string>? IncludedPlugins { get; set; }
        public List<string>? ExcludedPlugins { get; set; }

        public List<FunctionName>? IncludedFunctions
        {
            get => _includedFunctions;
            set => _includedFunctions = value;
        }

        public List<FunctionName>? ExcludedFunctions
        {
            get => _excludedFunctions;
            set => _excludedFunctions = value;
        }

        public List<string>? IncludedFunctionStrings
        {
            set => _includedFunctions = value?.Select(f => new FunctionName(f)).ToList();
        }

        public List<string>? ExcludedFunctionStrings
        {
            set => _excludedFunctions = value?.Select(f => new FunctionName(f)).ToList();
        }

        public bool ShouldIncludePlugin(string pluginName)
        {
            if (IncludedPlugins == null && ExcludedPlugins == null)
            {
                return true;
            }

            if (IncludedPlugins != null && !IncludedPlugins.Contains(pluginName))
            {
                return false;
            }

            if (ExcludedPlugins != null && ExcludedPlugins.Contains(pluginName))
            {
                return false;
            }

            return true;
        }

        public bool ShouldIncludeFunction(string functionName)
        {
            var function = new FunctionName(functionName);

            if (_includedFunctions == null && _excludedFunctions == null)
            {
                return true;
            }

            if (_includedFunctions != null && !_includedFunctions.Any(f => f.FullName == function.FullName))
            {
                return false;
            }

            if (_excludedFunctions != null && _excludedFunctions.Any(f => f.FullName == function.FullName))
            {
                return false;
            }

            return true;
        }
    }

    public class FunctionName : Tuple<string, string>
    {
        public FunctionName(string fullName) : base(
            fullName.Split('.').Length > 0 ? fullName.Split('.')[0] : string.Empty,
            fullName.Split('.').Length > 1 ? fullName.Split('.')[1] : string.Empty)
        {
        }

        public FunctionName(string pluginName, string functionName) : base(pluginName, functionName)
        {
        }

        public string PluginName => Item1;
        public string Name => Item2;

        public string FullName => $"{PluginName}.{Name}";

        public override string ToString()
        {
            return FullName;
        }
    }
}

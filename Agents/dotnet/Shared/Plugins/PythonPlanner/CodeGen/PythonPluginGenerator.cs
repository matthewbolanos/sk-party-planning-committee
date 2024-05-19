using Microsoft.SemanticKernel;
using PartyPlanning.Agents.Shared.Plugins.PythonPlanner.CodeGen.Models;
using PartyPlanning.Agents.Shared.Plugins.PythonPlanner.CodeGen.Extension;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Humanizer;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PartyPlanning.Agents.Shared.Plugins.PythonPlanner.CodeGen;

public class PythonPluginGenerator
{
    private const string PROMPT_SEPARATOR = "-";
    private const string EXECUTION_SEPARATOR = "__";
    private readonly HandlebarsPromptTemplateFactory _templateFactory;

    public PythonPluginGenerator()
    {
        _templateFactory = new HandlebarsPromptTemplateFactory(new (){
            RegisterCustomHelpers = HandlebarsPromptTemplateExtensions.RegisterCustomCreatePlanHelpers
        });
    }

    public async Task<string> GeneratePluginCodeAsync(
        Kernel kernel,
        PythonPluginGeneratorSettings pythonGeneratorSettings,
        CancellationToken cancellationToken = default)
    {
        // Prepare function and component metadata
        KernelPluginCollection filteredPluginsAndFunctions = kernel.Plugins.GetFunctions(pythonGeneratorSettings.FunctionFilters);
        List<PythonFunctionMetadata> availableFunctions = GetAvailableFunctionsManual(filteredPluginsAndFunctions, out var complexParameterSchemas);
            
        // Generate function stubs
        return await GeneratePythonPluginCodeAsync(
            kernel,
            availableFunctions,
            complexParameterSchemas,
            pythonGeneratorSettings,
            cancellationToken
        );
    }

    private async Task<string> GeneratePythonPluginCodeAsync(
        Kernel kernel,
        List<PythonFunctionMetadata> availableFunctions,
        Dictionary<PythonParameterMetadata, PythonParameterMetadata>? complexParameterSchemas,
        PythonPluginGeneratorSettings? pythonGeneratorSettings = null,
        CancellationToken cancellationToken = default)
    {
        // Construct prompt from Partials and Prompt Template
        var pythonScript = this.ConstructHandlebarsTemplate("PythonPlugins");

        // Render the prompt
        var promptTemplateConfig = new PromptTemplateConfig()
        {
            Template = pythonScript,
            TemplateFormat = HandlebarsPromptTemplateFactory.HandlebarsTemplateFormat,
        };


        var arguments = new KernelArguments()
            {
                { "isMock", pythonGeneratorSettings?.IsMock ?? true },
                { "functions", availableFunctions},
                { "nameDelimiter", "__"},
                { "complexSchemaDefinitions", complexParameterSchemas?.Select(kvp => kvp.Key) ?? []}
            };

        var handlebarsTemplate = this._templateFactory.Create(promptTemplateConfig);
        return await handlebarsTemplate!.RenderAsync(kernel, arguments, cancellationToken).ConfigureAwait(true);
    }

    private List<PythonFunctionMetadata> GetAvailableFunctionsManual(
    KernelPluginCollection pluginCollection,
    out Dictionary<PythonParameterMetadata, PythonParameterMetadata> complexParameterSchemas)
    {
        List<PythonFunctionMetadata> pythonFunctionMetadata = new();
        complexParameterSchemas = new Dictionary<PythonParameterMetadata, PythonParameterMetadata>();
        var queue = new Queue<PythonParameterMetadata>();

        foreach (var plugin in pluginCollection)
        {
            // TODO: find a better way to exclude the python plugin
            if (plugin.Name == "python")
            {
                continue;
            }

            foreach (var kernelFunction in plugin)
            {
                var inputProperties = new JsonObject();

                foreach (var parameter in kernelFunction.Metadata.Parameters)
                {
                    var parameterSchema = JsonObject.Create(parameter.Schema!.RootElement.Deserialize<JsonElement>());
                    if (parameter.IsRequired)
                    {
                        parameterSchema!.Add("required", true);
                    }
                    inputProperties[parameter.Name] = parameterSchema;
                }

                var inputSchemaJson = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = inputProperties
                };

                JsonElement schema = JsonDocument.Parse(inputSchemaJson.ToJsonString()).RootElement;
                PythonParameterMetadata arguments = new(plugin.Name, $"{kernelFunction.Name}_inputs", schema);
                queue.Enqueue(arguments);

                PythonParameterMetadata returnParameter = kernelFunction.Metadata.ReturnParameter.ToPythonParameterMetadata(
                    plugin.Name, kernelFunction.Name, kernelFunction.Metadata.ReturnParameter.Description);
                queue.Enqueue(returnParameter);

                pythonFunctionMetadata.Add(new PythonFunctionMetadata(
                    plugin.Name,
                    kernelFunction.Name,
                    arguments,
                    returnParameter,
                    kernelFunction.Description
                ));
            }
        }

        ProcessComplexTypeDefinitions(queue, complexParameterSchemas);

        return pythonFunctionMetadata;
    }


    // Extract any complex types or schemas for isolated render in prompt template
    private void ProcessComplexTypeDefinitions(
    Queue<PythonParameterMetadata> queue,
    Dictionary<PythonParameterMetadata, PythonParameterMetadata> complexParameterSchemas)
    {
        while (queue.Count > 0)
        {
            var currentParameter = queue.Dequeue();
            string pythonType = currentParameter.PythonType;

            // Check if the schema is an array
            if (pythonType == "List[any]")
            {
                // Perform logic to make the name singular
                string singular = currentParameter.Name.Singularize();

                // Create KernelParameterMetadata for the items in the array
                PythonParameterMetadata arrayItem = new(currentParameter.PluginName, singular, currentParameter.Schema.GetProperty("items"));
                currentParameter.ListItemMetadata = arrayItem;

                queue.Enqueue(arrayItem);
            }
            else if (pythonType == "any")
            {
                // Check if the schema has already been added
                if (complexParameterSchemas.ContainsKey(currentParameter))
                {
                    // Match name to existing schema
                    currentParameter.PythonType = complexParameterSchemas[currentParameter].PythonType;
                }
                else
                {
                    complexParameterSchemas.Add(currentParameter, currentParameter);

                    // Loop through the properties of the schema
                    if (currentParameter.Schema.TryGetProperty("properties", out var properties))
                    {
                        foreach (var property in properties.EnumerateObject())
                        {
                            // Create a new PythonParameterMetadata for each property
                            PythonParameterMetadata propertyMetadata = new(currentParameter.PluginName, property.Name, property.Value);
                            currentParameter.Properties.Add(propertyMetadata);
                            queue.Enqueue(propertyMetadata);
                        }
                    }
                }
            }
        }
    }



}
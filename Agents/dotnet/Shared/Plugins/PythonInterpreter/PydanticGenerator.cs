using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Humanizer;
using Microsoft.SemanticKernel;

public class PydanticGenerator
{
    private const string PROMPT_SEPARATOR = "-";
    private const string EXECUTION_SEPARATOR = "__";

    public string GeneratePluginCode(Kernel kernel, bool isMock = true)
    {
        // Loop through all of the plugins in the kernel
        Dictionary<string, Tuple<string,string>> componentSchemas = [];
        string functionStubs = "";

        foreach (var plugin in kernel.Plugins)
        {
            if (plugin.Name == "python")
            {
                continue;
            }

            // Loop through the functions in the plugin
            foreach (var function in plugin)
            {
                // Loop the function's parameters
                foreach (KernelParameterMetadata parameter in function.Metadata.Parameters)
                {
                    AddComponentSchema(plugin.Name, parameter.Name, parameter.Schema!.RootElement, componentSchemas);
                }

                // Get the function's return type
                AddComponentSchema(plugin.Name, function.Name+"_result", function.Metadata.ReturnParameter.Schema!.RootElement, componentSchemas);
            }

            // Generate function stubs
            functionStubs += GenerateFunctionStubsFromPlugin(plugin, componentSchemas, isMock) + "\n\n";
        }

        // Generate Pydantic models
        var pydanticModels = new List<string>();
        foreach (string schema in componentSchemas.Keys)
        {
            string pluginName = componentSchemas[schema].Item1;
            string functionName = componentSchemas[schema].Item2;
            string modelCode = JsonSchemaToPydanticModel(JsonSerializer.Deserialize<JsonObject>(schema)!, componentSchemas, functionName, pluginName);
            pydanticModels.Add(modelCode);
        }

        // Combine the generated code
        return $"from typing import List\n\n{string.Join("\n\n", pydanticModels)}\n\n{functionStubs}";
    }

    private static void AddComponentSchema(string pluginName, string name, JsonElement schema, Dictionary<string, Tuple<string,string>> componentSchemas)
    {
        schema.TryGetProperty("type", out var type);

        // Check if the schema is an out-of-the-box python primitive
        if (type.GetString() == "string" || type.GetString() == "number" || type.GetString() == "integer" || type.GetString() == "boolean")
        {
            return;
        }

        // Check if the schema is an array
        if (type.GetString() == "array")
        {
            // Perform logic to make the name singular
            string singular = name.Singularize();

            AddComponentSchema(pluginName, singular, schema.GetProperty("items"), componentSchemas);
            return;
        }

        // Check if the schema has already been added
        string objectSchema = JsonSerializer.Serialize(schema);
        if (componentSchemas.ContainsKey(objectSchema))
        {
            return;
        }

        // Add the schema to the dictionary
        componentSchemas.Add(objectSchema, new (pluginName, name));
    }

    private static string NormalizeNamespace(string namespaceName)
    {
        return Regex.Replace(namespaceName, "([a-z])([A-Z])", "$1_$2").ToLower();
    }

    private static string ToCamelCase(string snakeStr)
    {
        var components = snakeStr.Split('_');
        return components[0].Substring(0, 1).ToUpper() + components[0].Substring(1) + 
               string.Join("", components.Skip(1).Select(x => x.Substring(0, 1).ToUpper() + x.Substring(1)));
    }
    

    private static string JsonSchemaToPydanticModel(JsonObject schema, Dictionary<string,Tuple<string,string>> componentSchemas, string modelName, string namespaceName, bool isMock = true)
    {
        string fullModelName = $"{ToCamelCase(namespaceName)}{EXECUTION_SEPARATOR}{modelName}";
        var properties = schema["properties"]?.AsObject() ?? new JsonObject();
        var required = schema["required"]?.AsArray()?.Select(x => x!.ToString()).ToList() ?? [];

        var modelLines = new List<string> { $"class {fullModelName}:" };
        foreach (var prop in properties)
        {
            string propType = GetPydanticType(prop.Value!.Deserialize<JsonObject>(), componentSchemas, prop.Key);
            string formatComment = prop.Value["format"] != null ? $"  # format: {prop.Value["format"]}" : "";
            string defaultVal = required.Contains(prop.Key) ? "" : " = None";
            modelLines.Add($"    {prop.Key}: {propType}{defaultVal}{formatComment}");
        }

        return string.Join("\n", modelLines);
    }

    private static string GetPydanticType(JsonObject schema, Dictionary<string, Tuple<string,string>> componentSchemas, string propName, bool isMock = true)
    {
        var typeMapping = new Dictionary<string, string>
        {
            { "string", "str" },
            { "number", "float" },
            { "integer", "int" },
            { "boolean", "bool" }
        };

        string jsonType = schema["type"]?.ToString()!;
        if (jsonType == "array")
        {
            // Get the array of types from the type property of the schema
            JsonObject itemsSchema = schema["items"]?.AsObject();
            string itemsType = GetPydanticType(itemsSchema, componentSchemas, propName);
            return $"List[{itemsType}]";
        }
        if (jsonType == "object")
        {
            // Check if the object is a component schema
            string objectSchema = JsonSerializer.Serialize(schema);
            if (componentSchemas.ContainsKey(objectSchema))
            {
                string namespaceName = componentSchemas[objectSchema].Item1;
                string modelName = componentSchemas[objectSchema].Item2;
                return $"{ToCamelCase(namespaceName)}{EXECUTION_SEPARATOR}{modelName}";
            }

            string nestedModelName = char.ToUpper(propName[0]) + propName.Substring(1);
            return nestedModelName;
        }
        if (jsonType!= null && typeMapping.ContainsKey(jsonType))
        {
            return typeMapping[jsonType];
        }
        return "any";
    }

    private static string GenerateFunctionStubsFromPlugin(KernelPlugin plugin, Dictionary<string, Tuple<string,string>> componentSchemas, bool isMock = true)
    {
        string namespaceName = NormalizeNamespace(plugin.Name);
        var functionStubs = new List<string>();

        foreach (var function in plugin)
        {
            string functionName = $"{function.Name}";
            string returnType = "any";
            var paramDefs = new List<string>();

            foreach (var param in function.Metadata.Parameters)
            {
                string paramName;

                // Check if the parameter is a component schema
                string paramSchema = JsonSerializer.Serialize(param.Schema);
                if (componentSchemas.ContainsKey(paramSchema))
                {
                    paramName = componentSchemas[paramSchema].Item2;
                    namespaceName = componentSchemas[paramSchema].Item1;
                    paramDefs.Add($"{param.Name}: {ToCamelCase(namespaceName)}{EXECUTION_SEPARATOR}{paramName}");
                    continue;
                } else
                {
                    paramName = param.Name;
                    string paramType = GetPydanticType(param.Schema!.RootElement.Deserialize<JsonObject>()!, componentSchemas, paramName);
                    paramDefs.Add($"{paramName}: {paramType}");
                    functionName = functionName.Replace($"{{{paramName}}}", paramName.ToUpper());
                }
            }

            var returnSchema = function.Metadata.ReturnParameter.Schema!.RootElement;
            returnType = GetPydanticType(returnSchema.Deserialize<JsonObject>()!, componentSchemas, "any");

            string fullFunctionName = $"{namespaceName}{(isMock ? PROMPT_SEPARATOR : EXECUTION_SEPARATOR)}{functionName}";
            string functionDef = $"def {fullFunctionName}(";
            functionDef += "arguments: dict = None";
            // functionDef += string.Join(", ", paramDefs);
            functionDef += $") -> {returnType}:";
            if (isMock)
            {
                functionDef += "\n    pass\n";
            }
            else 
            {
                // functionDef += $@"        function_call_arguments = ";

                // // Add the parameters to the request data
                // functionDef += "{";
                // foreach (KernelParameterMetadata parameter in function.Metadata.Parameters)
                // {
                //     functionDef += $"'{parameter.Name}': {parameter.Name}, ";
                // }
                // if (function.Metadata.Parameters.Count > 0)
                // {
                //     functionDef = functionDef.Substring(0, functionDef.Length - 2);
                // }
                // functionDef += "}";

                // Add the request to the queue
                functionDef += $@"
        function_call_id = write_function_call('{plugin.Name}', '{function.Name}', arguments)
        if function_call_id != None:
            response = poll_for_results(function_call_id)
            return response
";
        }
            functionStubs.Add(functionDef);
        }

        string classDef = "";
        classDef += string.Join("\n", functionStubs.Select(line => $"{line}"));

        return classDef;
    }
}

//     private static string GetPydanticType(JsonElement schema, string propName)
//     {
//         var typeMapping = new Dictionary<string, string>
//         {
//             { "string", "str" },
//             { "number", "float" },
//             { "integer", "int" },
//             { "boolean", "bool" }
//         };

//         if (schema.TryGetProperty("type", out var typeElement))
//         {
//             JsonValueKind jsonType = typeElement.ValueKind!;
//             if (jsonType == JsonValueKind.Array)
//             {
//                 List<JsonElement> itemsSchema = [.. schema.GetProperty("types").EnumerateArray()];
//                 string itemsType = GetPydanticType(itemsSchema[0], propName);
//                 return $"List<{itemsType}>";
//             }
//             if (jsonType == JsonValueKind.Object)
//             {
//                 string nestedModelName = char.ToUpper(propName[0]) + propName[1..];
//                 return nestedModelName;
//             }
//             if (typeMapping.TryGetValue(typeElement.ToString()!, out string? value))
//             {
//                 return value;
//             }
//         }
//         return "Any";
//     }
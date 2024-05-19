// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Text;
using PartyPlanning.Agents.Shared.Plugins.PythonPlanner.CodeGen.Models;

namespace PartyPlanning.Agents.Shared.Plugins.PythonPlanner.CodeGen.Extension;

internal static class KernelParameterMetadataExtensions
{

    public static KernelParameterMetadata ParseJsonSchema(this KernelParameterMetadata parameter)
    {
        var schema = parameter.Schema!;

        var type = "object";
        if (schema.RootElement.TryGetProperty("type", out var typeNode))
        {
            type = typeNode.Deserialize<string>()!;
        }

        return parameter;
    }

    public static string ToJsonString(this JsonElement jsonProperties)
    {
        return JsonSerializer.Serialize(jsonProperties, JsonOptionsCache.WriteIndented);
    }

    public static string GetSchemaTypeName(this KernelParameterMetadata parameter)
    {
        var schemaType = parameter.Schema?.RootElement.TryGetProperty("type", out var typeElement) is true ? typeElement.ToString() : "object";
        return $"{parameter.Name}-{schemaType}";
    }
    public static PythonParameterMetadata ToPythonParameterMetadata(this KernelParameterMetadata parameter, string pluginName, string functionName) =>
        new(pluginName, functionName, parameter.Schema!.RootElement);

    public static PythonParameterMetadata ToPythonParameterMetadata(this KernelReturnParameterMetadata parameter, string pluginName, string functionName, string? description = null) =>
        new(pluginName, $"{functionName}_return", parameter.Schema!.RootElement, description);
}
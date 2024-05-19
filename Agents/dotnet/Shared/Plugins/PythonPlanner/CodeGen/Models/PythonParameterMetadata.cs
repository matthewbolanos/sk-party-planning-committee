// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace PartyPlanning.Agents.Shared.Plugins.PythonPlanner.CodeGen.Models;

public class PythonParameterMetadata(string pluginName, string name, JsonElement schema, string? description = null)
{
    public string PluginName { get; set; } = pluginName;
    public string Name { get; set; } = name;
    public JsonElement Schema { get; set; } = schema;
    public bool IsRequired  {
        get {
            try {
                return Schema.GetProperty("required").GetBoolean();
            } catch (KeyNotFoundException) {
                return false;
            }
        }
    }

    public string Description {
        get {
            if (description != null)
            {
                return description;
            }

            try {
                return Schema.GetProperty("description").GetString()!;
            } catch (KeyNotFoundException) {
                return "";
            }
        }
    }

    /// <summary>
    /// The type of the parameter if it is a List
    /// </summary>
    public PythonParameterMetadata? ListItemMetadata { get; set; }

    /// <summary>
    /// If this is a complex type, this will contain the properties of the complex type.
    /// </summary>
    [JsonPropertyName("properties")]
    public List<PythonParameterMetadata> Properties { get; set; } = [];

    // Override the Equals method to compare the property values
    public override bool Equals(object? obj)
    {
        // Check to make sure the object is the expected type
        if (obj is not PythonParameterMetadata other)
        {
            return false;
        }

        if (this.GetPythonType() == "any" && other.GetPythonType() == "any"){
            return ArePropertiesEqual(this.Properties, other.Properties);
        }
        if (this.GetPythonType() == "List" && other.GetPythonType() == "List"){
            return ListItemMetadata!.Equals(other.ListItemMetadata!);
        }
        if (this.GetType() == other.GetType())
        {
            return true;
        }
        return false;
    }

    // A helper method to compare two lists of KernelParameterMetadata
    private static bool ArePropertiesEqual(List<PythonParameterMetadata> list1, List<PythonParameterMetadata> list2)
    {
        // Check if the lists are null or have different lengths
        if (list1 is null || list2 is null || list1.Count != list2.Count)
        {
            return false;
        }

        // Compare the elements of the lists by comparing the Name and ParameterType properties
        for (int i = 0; i < list1.Count; i++)
        {
            if (!list1[i].Name.Equals(list2[i].Name, System.StringComparison.Ordinal) || JsonSerializer.Serialize(list1[i].Schema)!.Equals(JsonSerializer.Serialize(list2[i].Schema)))
            {
                return false;
            }
        }

        // If all elements are equal, return true
        return true;
    }

    // Override the GetHashCode method to generate a hash code based on the property values
    public override int GetHashCode()
    {
        HashCode hash = default;

        // Objects shouldn't be shared between plugins
        hash.Add(PluginName);

        // Combine the Name and ParameterType properties into one hash code
        hash.Add(JsonSerializer.Serialize(Schema)!);

        return hash.ToHashCode();
    }

    private string? _PythonType = null;
    public string PythonType {
        get {
            if (_PythonType == null)
            {
                return  GetPythonType();
            }
            return _PythonType;
        }
        set {
            _PythonType = value;
        }
    }

    private string GetPythonType()
    {
        string type;
        try {
            var typeProperty = Schema.GetProperty("type");
            if (typeProperty.ValueKind == JsonValueKind.Array)
            {
                // Hack: We are assuming that the first element of the array is the type
                type = typeProperty[0].GetString()!;
            }
            else
            {
                type = typeProperty.GetString()!;
            }
        } catch (KeyNotFoundException) {
            return "any";
        }

        // Check if the schema is an out-of-the-box python primitive
        if (type == "string")
        {
            return "str";
        }
        else if (type == "number")
        {
            return "float";
        }
        else if (type == "integer")
        {
            return "int";
        }
        else if (type == "boolean")
        {
            return "bool"; 
        }
        else if (type == "array")
        {
            if (ListItemMetadata == null)
            {
                return "List[any]";
            }
            return "List["+ListItemMetadata!.PythonType+"]";
        }
        else
        {
            if (Properties.Count > 0)
            {
                return ToPascalCase(PluginName) + ToPascalCase(Name);
            }
            return "any";
        }
    }

    private string ToPascalCase(string name)
    {
        // Convert from camelCase to PascalCase
        if (name.Length > 0 && char.IsLower(name[0]))
        {
            name = char.ToUpper(name[0]) + name[1..];
        }

        // Convert from snake_case to PascalCase
        return string.Join("", name.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(s => char.ToUpper(s[0]) + s[1..]));
    }
}
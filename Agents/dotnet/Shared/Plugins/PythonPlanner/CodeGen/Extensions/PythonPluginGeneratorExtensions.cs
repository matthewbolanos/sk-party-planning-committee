// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using System.Text;

namespace PartyPlanning.Agents.Shared.Plugins.PythonPlanner.CodeGen.Extension;

/// <summary>
/// Extension methods for the <see cref="HandlebarsPlanner"/> interface.
/// </summary>
internal static class PythonPluginGeneratorExtensions
{
    /// <summary>
    /// Reads the template for the given file name.
    /// </summary>
    /// <param name="planner">The handlebars planner.</param>
    /// <param name="fileName">The name of the file to read.</param>
    /// <param name="additionalNameSpace">The name of the additional namespace.</param>
    /// <returns>The content of the file as a string.</returns>
    public static string ReadPlannerTemplate(this PythonPluginGenerator pythonPluginGenerator, string fileName, string? additionalNameSpace = "")
    {
        using var stream = pythonPluginGenerator.ReadPlannerTemplateStream(fileName, additionalNameSpace);
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }

    /// <summary>
    /// Reads the template stream for the given file name.
    /// </summary>
    /// <param name="planner">The handlebars planner.</param>
    /// <param name="fileName">The name of the file to read.</param>
    /// <param name="additionalNamespace">The name of the additional namespace.</param>
    /// <returns>The stream for the given file name.</returns>
    public static Stream ReadPlannerTemplateStream(this PythonPluginGenerator pythonPluginGenerator, string fileName, string? additionalNamespace = "")
    {
        // output all resources in the assembly
        var resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
        

        var assembly = Assembly.GetExecutingAssembly();
        var plannerNamespace = pythonPluginGenerator.GetType().Namespace;
        var targetNamespace = !string.IsNullOrEmpty(additionalNamespace) ? $".{additionalNamespace}" : string.Empty;
        var resourceName = $"{plannerNamespace}{targetNamespace}.{fileName}";

        return assembly.GetManifestResourceStream(resourceName)!;
    }

    /// <summary>
    /// Constructs a Handblebars template from the given file name and corresponding partials, if any.
    /// Partials must be contained in a directory following the naming convention: "Partials" and loaded inline first to avoid reference errors.
    /// </summary>
    /// <param name="planner">The handlebars planner.</param>
    /// <param name="templateName">The name of the file to read.</param>
    /// <param name="additionalNamespace">The name of the additional namespace.</param>
    /// <param name="templateOverride">Override for Create Plan template.</param>
    /// <returns>The constructed template.</returns>
    public static string ConstructHandlebarsTemplate(
        this PythonPluginGenerator pythonPluginGenerator,
        string templateName,
        string? additionalNamespace = "",
        string? templateOverride = null)
    {
        var partials = pythonPluginGenerator.ReadAllTemplatePartials(templateName, additionalNamespace);
        var template = !string.IsNullOrEmpty(templateOverride) ? templateOverride : pythonPluginGenerator.ReadPlannerTemplate($"{templateName}.handlebars", additionalNamespace);
        return partials + template;
    }

    /// <summary>
    /// Reads all embedded Handlebars template partials from the Handlebars Planner `TemplatePartials` namespace and concatenates their contents.
    /// </summary>
    /// <param name="planner">The handlebars planner.</param>
    /// <param name="templateName">The name of the parent Handlebars template file.</param>
    /// <param name="additionalNamespace">The name of the additional namespace.</param>
    /// <returns>The concatenated content of the embedded partials within the Handlebars Planner namespace.</returns>
    public static string ReadAllTemplatePartials(this PythonPluginGenerator pythonPluginGenerator, string templateName, string? additionalNamespace = "")
    {
        var assembly = Assembly.GetExecutingAssembly();
        var plannerNamespace = pythonPluginGenerator.GetType().Namespace;
        var parentNamespace = !string.IsNullOrEmpty(additionalNamespace) ? $"{plannerNamespace}.{additionalNamespace}" : plannerNamespace;
        var targetNamespace = $"{parentNamespace}.Partials";

        var resourceNames = assembly.GetManifestResourceNames()
            .Where(name =>
                name.StartsWith(targetNamespace, StringComparison.CurrentCulture)
                && name.EndsWith(".handlebars", StringComparison.CurrentCulture))
            // Sort by the number of dots in the name (subdirectory depth), loading subdirectories first, as the outer partials have dependencies on the inner ones.
            .OrderByDescending(name => name.Count(c => c == '.'))
            // then by the name itself
            .ThenBy(name => name);

        var stringBuilder = new StringBuilder();
        foreach (var resourceName in resourceNames)
        {
            using Stream? resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream is not null)
            {
                using var reader = new StreamReader(resourceStream);
                stringBuilder.AppendLine(reader.ReadToEnd());
            }
        }

        return stringBuilder.ToString();
    }
}
// Copyright (c) Microsoft. All rights reserved.

using HandlebarsDotNet;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using static Microsoft.SemanticKernel.PromptTemplates.Handlebars.HandlebarsPromptTemplateOptions;

namespace PartyPlanning.Agents.Shared.Plugins.PythonPlanner.CodeGen.Extension;

/// <summary>
/// Provides extension methods for rendering Handlebars templates in the context of a Semantic Kernel.
/// </summary>
internal sealed class HandlebarsPromptTemplateExtensions
{
    public static void RegisterCustomCreatePlanHelpers(
        RegisterHelperCallback registerHelper,
        HandlebarsPromptTemplateOptions options,
        KernelArguments executionContext
    )
    {
        registerHelper("toPascalCase", static (Context context, Arguments arguments) =>
        {
            string name = (string)arguments[0];

            // Convert from camelCase to PascalCase
            if (name.Length > 0 && char.IsLower(name[0]))
            {
                name = char.ToUpper(name[0]) + name[1..];
            }

            // Convert from snake_case to PascalCase
            return string.Join("", name.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(s => char.ToUpper(s[0]) + s[1..]));
        });
        
        registerHelper("toSnakeCase", static (Context context, Arguments arguments) =>
        {
            string name = (string)arguments[0];

            // Convert from camelCase to snake_case
            return string.Concat(name.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
        });
    }
}
using System.Reflection;

public class OpenApiResourceService
{
    /// <summary>
    /// Gets the OpenAPI resource from the assembly.
    /// </summary>
    /// <param name="OpenApiResourceName">The name of the OpenAPI Resource (e.g., myPlugin.swagger.json)</param>
    /// <returns>The contents of the OpenAPI Resource</returns>
    /// <exception cref="Exception"></exception>
    public string GetOpenApiResource(string OpenApiResourceName)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string? fullResourceName = assembly.GetManifestResourceNames().FirstOrDefault(x =>
            x.EndsWith(OpenApiResourceName)
        );

        if (fullResourceName == null)
        {
            throw new Exception($"Resource {OpenApiResourceName} not found in assembly {assembly.FullName}");
        }

        using (Stream stream = assembly.GetManifestResourceStream(fullResourceName)!)
        using (StreamReader reader = new StreamReader(stream))
        {
            return reader.ReadToEnd();
        }
    }
}

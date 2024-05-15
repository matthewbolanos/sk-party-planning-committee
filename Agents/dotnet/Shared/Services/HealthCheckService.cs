using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

public class HealthCheckService
{
    private readonly HttpClient _httpClient;

    public HealthCheckService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetHealthyEndpointAsync(IEnumerable<string> endpoints, string healthCheckPath = "/health")
    {
        foreach (var endpoint in endpoints)
        {
            if (await IsEndpointHealthyAsync(endpoint, healthCheckPath))
            {
                return endpoint;
            }
        }

        throw new Exception("All endpoints are down.");
    }

    private async Task<bool> IsEndpointHealthyAsync(string endpoint, string healthCheckPath = "/health")
    {
        try
        {
            var response = await _httpClient.GetAsync($"{endpoint}{healthCheckPath}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

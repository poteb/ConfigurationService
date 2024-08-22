using System.Text.Json;

namespace pote.Config.Middleware;

public interface ISecretResolver
{
    Task<T> ResolveSecret<T>(string secret);
}

public class SecretResolver : ISecretResolver
{
    private readonly BuilderConfiguration _configuration;
    private readonly Func<HttpClient> _clientProvider;

    public SecretResolver(BuilderConfiguration configuration, Func<HttpClient> clientProvider)
    {
        _configuration = configuration;
        _clientProvider = clientProvider;
    }

    public async Task<T> ResolveSecret<T>(string secret)
    {
        var client = _clientProvider();
        var response = await client.GetAsync($"{_configuration.ApiUri}/secrets/{secret}");
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Error getting secret {secret} from API. Status code: {response.StatusCode}");
        var responseBody = await response.Content.ReadAsStringAsync();
        var secretObj = JsonSerializer.Deserialize<T>(responseBody);
        return secretObj;
    }
}
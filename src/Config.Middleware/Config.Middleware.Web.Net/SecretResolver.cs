using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using pote.Config.Middleware.Secrets;
using pote.Config.Shared;

namespace pote.Config.Middleware.Web;

public class SecretResolver : ISecretResolver
{
    private const string RefPatternQuotes = "\\$refs:(?<ref>[^#]*)#?[^\"]*";
    private readonly BuilderConfiguration _configuration;
    private readonly Func<HttpClient> _clientProvider;

    public SecretResolver(BuilderConfiguration configuration, Func<HttpClient> clientProvider)
    {
        _configuration = configuration;
        _clientProvider = clientProvider;
    }

    public string ResolveSecret(string secret)
    {
        var secretName = secret;
        var match = Regex.Match(secret, RefPatternQuotes);
        if (match.Success)
            secretName = match.Groups["ref"].Value;
        var client = _clientProvider();
        var request = new SecretValueRequest
        {
            SecretName = secretName,
            Application = _configuration.Application,
            Environment = _configuration.Environment
        };
        var webRequest = new HttpRequestMessage(HttpMethod.Post, $"{_configuration.ApiUri}/Secrets/")
        {
            Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
        };
        var response = client.Send(webRequest);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Error getting secret {secretName} from API. Status code: {response.StatusCode}");
        using var reader = new StreamReader(response.Content.ReadAsStream());
        var jsonResponse = reader.ReadToEnd();
        var responseBody = JsonSerializer.Deserialize<SecretValueResponse>(jsonResponse, new JsonSerializerOptions{PropertyNameCaseInsensitive = true});
        return responseBody!.Value;
    }
    
    public async Task<string> ResolveSecretAsync(string secret)
    {
        var secretName = secret;
        var match = Regex.Match(secret, RefPatternQuotes);
        if (match.Success)
            secretName = match.Groups["ref"].Value;
        var client = _clientProvider();
        var request = new SecretValueRequest
        {
            SecretName = secretName,
            Application = _configuration.Application,
            Environment = _configuration.Environment
        };
        var response = await client.PostAsJsonAsync($"{_configuration.ApiUri}/Secrets/", request);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Error getting secret {secretName} from API. Status code: {response.StatusCode}");
        var responseBody = await response.Content.ReadFromJsonAsync<SecretValueResponse>();
        return responseBody!.Value;
    }
}
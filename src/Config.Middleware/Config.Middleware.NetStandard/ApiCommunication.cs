using System.Text;
using System.Text.Json;
using pote.Config.Shared;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace pote.Config.Middleware;

public interface IApiCommunication
{
    Task<ParseResponse?> GetConfiguration(ParseRequest request);
}

internal class ApiCommunication : IApiCommunication
{
    private readonly string _rootApiUri;
    private readonly HttpClient _client;

    public ApiCommunication(string rootApiUri, HttpClient client)
    {
        _rootApiUri = rootApiUri;
        _client = client;
    }

    public async Task<ParseResponse?> GetConfiguration(ParseRequest request)
    {
        var response = await _client.PostAsync($"{_rootApiUri}/Configuration", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        return await JsonSerializer.DeserializeAsync<ParseResponse>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}
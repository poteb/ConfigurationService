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
    private readonly string _apiUri;
    private readonly HttpClient _client;

    public ApiCommunication(string apiUri, HttpClient client)
    {
        _apiUri = apiUri;
        _client = client;
    }

    public async Task<ParseResponse?> GetConfiguration(ParseRequest request)
    {
        var response = await _client.PostAsync($"{_apiUri}/Configuration", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        return await JsonSerializer.DeserializeAsync<ParseResponse>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}
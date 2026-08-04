using System.Net.Http.Json;
using System.Text;
using pote.Config.Admin.WebClient.Model;
using pote.Config.Shared;

namespace pote.Config.Admin.WebClient.Services;

public interface IApiService
{
    Task<ApiCallResponse<ParseResponse>> TestConfiguration(string json, Application application, ConfigEnvironment environment);
}

public class ApiService : ApiServiceBase, IApiService
{
    private readonly IHttpClientFactory _clientFactory;

    public ApiService(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task<ApiCallResponse<ParseResponse>> TestConfiguration(string json, Application application, ConfigEnvironment environment)
    {
        try
        {
            using var client = _clientFactory.CreateClient("AdminApi");
            var request = new ParseRequest(application.Id, environment.Id, json);
            var response = await client.PostAsJsonAsync("Configuration", request);
            if (!response.IsSuccessStatusCode)
                return DefaultUnsuccessfulResponse(new ParseResponse(), (int)response.StatusCode);
            var data = await response.Content.ReadFromJsonAsync<ParseResponse>();
            return new ApiCallResponse<ParseResponse> { IsSuccess = true, Response = data };
        }
        catch (Exception ex)
        {
            return DefaultExceptionResponse(new ParseResponse(), "Error testing configuration", ex);
        }
    }
}
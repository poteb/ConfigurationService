using System.Net.Http.Json;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.Admin.WebClient.Model;

namespace pote.Config.Admin.WebClient.Services;

public interface IDependencyGraphApiService
{
    Task<ApiCallResponse<DependencyGraphResponse>> GetDependencyGraph();
}

public class DependencyGraphApiService : ApiServiceBase, IDependencyGraphApiService
{
    private readonly IHttpClientFactory _clientFactory;

    public DependencyGraphApiService(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task<ApiCallResponse<DependencyGraphResponse>> GetDependencyGraph()
    {
        try
        {
            using var client = _clientFactory.CreateClient("AdminApi");
            var response = await client.GetAsync("DependencyGraph");
            if (!response.IsSuccessStatusCode)
                return DefaultUnsuccessfulResponse(new DependencyGraphResponse(), (int)response.StatusCode);
            
            var content = await response.Content.ReadFromJsonAsync<DependencyGraphResponse>() ?? new DependencyGraphResponse();
            return new ApiCallResponse<DependencyGraphResponse> { IsSuccess = true, Response = content };
        }
        catch (Exception ex)
        {
            return DefaultExceptionResponse(new DependencyGraphResponse(), "Error getting data from API", ex);
        }
    }
}
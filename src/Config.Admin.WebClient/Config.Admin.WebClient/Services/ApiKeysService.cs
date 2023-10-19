using System.Net.Http.Json;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.Admin.WebClient.Model;

namespace pote.Config.Admin.WebClient.Services;

public interface IApiKeysService
{
    Task<ApiCallResponse<ApiKeysResponse>> GetApiKeys();
    Task<ApiCallResponse<object>> SaveApiKeys(ApiKeys settings);
}

public class ApiKeysService : ApiServiceBase, IApiKeysService
{
    private readonly IHttpClientFactory _clientFactory;

    public ApiKeysService(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task<ApiCallResponse<ApiKeysResponse>> GetApiKeys()
    {
        try
        {
            using var client = _clientFactory.CreateClient("AdminApi");
            var response = await client.GetAsync("ApiKeys");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<ApiKeysResponse>() ?? new ApiKeysResponse();
                return new ApiCallResponse<ApiKeysResponse> { IsSuccess = true, Response = content };
            }

            return DefaultUnsuccessfulResponse(new ApiKeysResponse(), (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            return DefaultExceptionResponse(new ApiKeysResponse(), "Error getting data from API", ex);
        }
    }

    public async Task<ApiCallResponse<object>> SaveApiKeys(ApiKeys apiKeys)
    {
        try
        {
            using var client = _clientFactory.CreateClient("AdminApi");
            await client.PostAsJsonAsync("ApiKeys", Mappers.ApiKeysMapper.ToApi(apiKeys));

            return new ApiCallResponse<object> { IsSuccess = true };
        }
        catch (Exception ex)
        {
            return DefaultExceptionResponse(new object(), "Error saving API keys", ex);
        }
    }
}
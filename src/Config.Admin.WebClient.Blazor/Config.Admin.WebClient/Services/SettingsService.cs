using System.Net.Http.Json;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.Admin.WebClient.Model;

namespace pote.Config.Admin.WebClient.Services;

public interface ISettingsService
{
    Task<ApiCallResponse<SettingsResponse>> GetSettings();
    Task<ApiCallResponse<object>> SaveSettings(Settings settings);
}

public class SettingsService : ApiServiceBase, ISettingsService
{
    private readonly IHttpClientFactory _clientFactory;

    public SettingsService(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }
    
    public async Task<ApiCallResponse<SettingsResponse>> GetSettings()
    {
        try
        {
            using var client = _clientFactory.CreateClient("AdminApi");
            var response = await client.GetAsync("Settings");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<SettingsResponse>() ?? new SettingsResponse();
                return new ApiCallResponse<SettingsResponse> { IsSuccess = true, Response = content };
            }

            return DefaultUnsuccessfulResponse(new SettingsResponse(), (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            return DefaultExceptionResponse(new SettingsResponse(), "Error getting data from API", ex);
        }
    }

    public async Task<ApiCallResponse<object>> SaveSettings(Settings settings)
    {
        try
        {
            using var client = _clientFactory.CreateClient("AdminApi");
            await client.PostAsJsonAsync("Settings", Mappers.SettingsMapper.ToApi(settings));

            return new ApiCallResponse<object> { IsSuccess = true };
        }
        catch (Exception ex)
        {
            return DefaultExceptionResponse(new object(), "Error saving settings", ex);
        }
    }
}
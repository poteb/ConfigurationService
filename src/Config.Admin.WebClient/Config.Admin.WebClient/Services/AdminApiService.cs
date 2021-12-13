using System.Net.Http.Json;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.Admin.WebClient.Model;

namespace pote.Config.Admin.WebClient.Services
{
    public interface IAdminApiService
    {
        Task<ApiCallResponse<EnvironmentsResponse>> GetEnvironments();
        Task<ApiCallResponse<SystemsResponse>> GetSystems();
        Task<ApiCallResponse<object>> SaveEnvironments(List<ConfigEnvironment> environments);
        Task<ApiCallResponse<object>> SaveSystems(List<ConfigSystem> systems);
    }

    public class AdminApiService : IAdminApiService
    {
        private readonly HttpClient _client;

        public AdminApiService(HttpClient client)
        {
            _client = client;
        }

        public async Task<ApiCallResponse<EnvironmentsResponse>> GetEnvironments()
        {
            try
            {
                var response = await _client.GetAsync("Environments");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadFromJsonAsync<EnvironmentsResponse>() ?? new EnvironmentsResponse();
                    return new ApiCallResponse<EnvironmentsResponse> { IsSuccess = true, Response = content };
                }

                return DefaultUnsuccessfullResponse(new EnvironmentsResponse(), $"Call was unsuccessfull, error code: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return DefaultExceptionResponse(new EnvironmentsResponse(), "Error getting data from API", ex);
            }
        }

        public async Task<ApiCallResponse<SystemsResponse>> GetSystems()
        {
            try
            {
                var response = await _client.GetAsync("Systems");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadFromJsonAsync<SystemsResponse>() ?? new SystemsResponse();
                    return new ApiCallResponse<SystemsResponse> { IsSuccess = true, Response = content };
                }

                return DefaultUnsuccessfullResponse(new SystemsResponse(), $"Call was unsuccessfull, error code: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return DefaultExceptionResponse(new SystemsResponse(), "Error getting data from API", ex);
            }
        }

        public async Task<ApiCallResponse<object>> SaveEnvironments(List<ConfigEnvironment> environments)
        {
            try
            {
                foreach (var environment in environments)
                {
                    if (!environment.IsDeleted)
                        await _client.PostAsJsonAsync("Environments", Mappers.EnvironmentMapper.ToApi(environment));
                    else
                        await _client.DeleteAsync($"Environments?id={environment.Id}");
                }

                return new ApiCallResponse<dynamic> { IsSuccess = true };
            }
            catch (Exception ex)
            {
                return DefaultExceptionResponse(new object(), "Error saving environments", ex);
            }
        }

        public async Task<ApiCallResponse<object>> SaveSystems(List<ConfigSystem> systems)
        {
            try
            {
                foreach (var system in systems)
                {
                    if (!system.IsDeleted)
                        await _client.PostAsJsonAsync("Systems", Mappers.SystemMapper.ToApi(system));
                    else
                        await _client.DeleteAsync($"Systems?id={system.Id}");
                }

                return new ApiCallResponse<dynamic> { IsSuccess = true };
            }
            catch (Exception ex)
            {
                return DefaultExceptionResponse(new object(), "Error saving environments", ex);
            }
        }

        private static ApiCallResponse<T> DefaultExceptionResponse<T>(T response, string errorMessage, Exception ex)
        {
            return new ApiCallResponse<T> { Response = response, ErrorMessage = errorMessage, Exception = ex };
        }

        private static ApiCallResponse<T> DefaultUnsuccessfullResponse<T>(T response, string errorMessage)
        {
            return new ApiCallResponse<T> { Response = response, ErrorMessage = errorMessage };
        }
    }
}
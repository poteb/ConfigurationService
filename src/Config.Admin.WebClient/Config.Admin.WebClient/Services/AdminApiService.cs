using System.Net.Http.Json;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.Admin.WebClient.Model;

namespace pote.Config.Admin.WebClient.Services
{
    public interface IAdminApiService
    {
        Task<ApiCallResponse<EnvironmentsResponse>> GetEnvironments();
        Task<SystemsResponse> GetSystems();
        Task SaveEnvironments(List<Api.Model.Environment> toApi, List<string> deleted);
        Task SaveSystems(List<Api.Model.System> toApi, List<string> deleted);
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
                    return new ApiCallResponse<EnvironmentsResponse>
                    {
                        IsSuccess = true,
                        Response = content
                    };
                }

                return new ApiCallResponse<EnvironmentsResponse>
                {
                    Response = new EnvironmentsResponse(),
                    ErrorMessage = $"Call was unsuccessfull, error code: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                return new ApiCallResponse<EnvironmentsResponse>
                {
                    Response = new EnvironmentsResponse(),
                    ErrorMessage = "Error getting data from API",
                    Exception = ex
                };
            }
        }

        public async Task<SystemsResponse> GetSystems()
        {
            var response = await _client.GetAsync("Systems");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SystemsResponse>() ?? new SystemsResponse();
            }

            return new SystemsResponse();
        }

        public async Task SaveEnvironments(List<Api.Model.Environment> environments, List<string> deleted)
        {
            foreach (var environment in environments)
            {
                await _client.PostAsJsonAsync("Environments", environment);
            }

            foreach (var id in deleted)
            {
                await _client.DeleteAsync($"Environments?id={id}");
            }
        }

        public async Task SaveSystems(List<Api.Model.System> systems, List<string> deleted)
        {
            foreach (var system in systems)
            {
                await _client.PostAsJsonAsync("Systems", system);
            }

            foreach (var id in deleted)
            {
                await _client.DeleteAsync($"Systems?id={id}");
            }
        }
    }
}
using System.Net.Http.Json;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.Admin.WebClient.Model;

namespace pote.Config.Admin.WebClient.Services
{
    public interface IAdminApiService
    {
        Task<ApiCallResponse<ConfigurationResponse>> GetConfiguration(string gid);
        Task<ApiCallResponse<ConfigurationsResponse>> GetConfigurations();
        Task<ApiCallResponse<EnvironmentsResponse>> GetEnvironments();
        Task<ApiCallResponse<ApplicationsResponse>> GetApplications();
        Task<ApiCallResponse<object>> SaveEnvironments(List<ConfigEnvironment> environments);
        Task<ApiCallResponse<object>> SaveApplications(List<Application> applications);
        Task<ApiCallResponse<object>> SaveConfiguration(ConfigurationHeader configuration);
    }

    public class AdminApiService : ApiServiceBase, IAdminApiService
    {
        private readonly IHttpClientFactory _clientFactory;

        public AdminApiService(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<ApiCallResponse<ConfigurationResponse>> GetConfiguration(string gid)
        {
            try
            {
                using var client = _clientFactory.CreateClient("AdminApi");
                var response = await client.GetAsync($"Configurations/{gid}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadFromJsonAsync<ConfigurationResponse>() ?? new ConfigurationResponse();
                    return new ApiCallResponse<ConfigurationResponse> { IsSuccess = true, Response = content };
                }

                return DefaultUnsuccessfullResponse(new ConfigurationResponse(), (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                return DefaultExceptionResponse(new ConfigurationResponse(), "Error getting data from API", ex);
            }
        }

        public async Task<ApiCallResponse<ConfigurationsResponse>> GetConfigurations()
        {
            try
            {
                using var client = _clientFactory.CreateClient("AdminApi");
                var response = await client.GetAsync("Configurations");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadFromJsonAsync<ConfigurationsResponse>() ?? new ConfigurationsResponse();
                    return new ApiCallResponse<ConfigurationsResponse> { IsSuccess = true, Response = content };
                }
                
                return DefaultUnsuccessfullResponse(new ConfigurationsResponse(), (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                return DefaultExceptionResponse(new ConfigurationsResponse(), "Error getting data from API", ex);
            }
        }

        public async Task<ApiCallResponse<EnvironmentsResponse>> GetEnvironments()
        {
            try
            {
                using var client = _clientFactory.CreateClient("AdminApi");
                var response = await client.GetAsync("Environments");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadFromJsonAsync<EnvironmentsResponse>() ?? new EnvironmentsResponse();
                    return new ApiCallResponse<EnvironmentsResponse> { IsSuccess = true, Response = content };
                }

                return DefaultUnsuccessfullResponse(new EnvironmentsResponse(), (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                return DefaultExceptionResponse(new EnvironmentsResponse(), "Error getting data from API", ex);
            }
        }

        public async Task<ApiCallResponse<ApplicationsResponse>> GetApplications()
        {
            try
            {
                using var client = _clientFactory.CreateClient("AdminApi");
                var response = await client.GetAsync("Applications");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadFromJsonAsync<ApplicationsResponse>() ?? new ApplicationsResponse();
                    return new ApiCallResponse<ApplicationsResponse> { IsSuccess = true, Response = content };
                }

                return DefaultUnsuccessfullResponse(new ApplicationsResponse(), (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                return DefaultExceptionResponse(new ApplicationsResponse(), "Error getting data from API", ex);
            }
        }

        public async Task<ApiCallResponse<object>> SaveEnvironments(List<ConfigEnvironment> environments)
        {
            try
            {
                using var client = _clientFactory.CreateClient("AdminApi");
                foreach (var environment in environments)
                {
                    if (!environment.IsDeleted)
                        await client.PostAsJsonAsync("Environments", Mappers.EnvironmentMapper.ToApi(environment));
                    else
                        await client.DeleteAsync($"Environments?id={environment.Id}");
                }

                return new ApiCallResponse<object> { IsSuccess = true };
            }
            catch (Exception ex)
            {
                return DefaultExceptionResponse(new object(), "Error saving environments", ex);
            }
        }

        public async Task<ApiCallResponse<object>> SaveApplications(List<Application> applications)
        {
            try
            {
                using var client = _clientFactory.CreateClient("AdminApi");
                foreach (var application in applications)
                {
                    if (!application.IsDeleted)
                        await client.PostAsJsonAsync("Applications", Mappers.ApplicationMapper.ToApi(application));
                    else
                        await client.DeleteAsync($"Applications?id={application.Id}");
                }

                return new ApiCallResponse<object> { IsSuccess = true };
            }
            catch (Exception ex)
            {
                return DefaultExceptionResponse(new object(), "Error saving environments", ex);
            }
        }

        public async Task<ApiCallResponse<object>> SaveConfiguration(ConfigurationHeader header)
        {
            try
            {
                using var client = _clientFactory.CreateClient("AdminApi");
                var apiConfiguration = Mappers.ConfigurationMapper.ToApi(header);
                await client.PostAsJsonAsync("Configurations", apiConfiguration);
                return new ApiCallResponse<object> { IsSuccess = true };
            }
            catch (Exception ex)
            {
                return DefaultExceptionResponse(new object(), "Error saving configuration header", ex);
            }
        }
    }
}
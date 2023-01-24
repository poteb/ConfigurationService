using System.Net.Http.Json;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.Admin.WebClient.Model;

namespace pote.Config.Admin.WebClient.Services
{
    public interface IAdminApiService
    {
        Task<ApiCallResponse<ConfigurationResponse>> GetConfiguration(string gid);
        Task<ApiCallResponse<ConfigurationsResponse>> GetConfigurations();
        Task<ApiCallResponse<HeaderHistoryResponse>> GetHeaderHistory(string gid, int page, int pageSize);
        Task<ApiCallResponse<ConfigurationHistoryResponse>> GetConfigurationHistory(string headerId, string gid, int page, int pageSize);
        Task<ApiCallResponse<EnvironmentsResponse>> GetEnvironments();
        Task<ApiCallResponse<ApplicationsResponse>> GetApplications();
        Task<ApiCallResponse<object>> SaveEnvironments(List<ConfigEnvironment> environments);
        Task<ApiCallResponse<object>> SaveApplications(List<Application> applications);
        Task<ApiCallResponse<object>> SaveConfiguration(ConfigurationHeader configuration);
        Task<ApiCallResponse<bool>> DeleteConfiguration(string id, bool permanent);
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

                return DefaultUnsuccessfulResponse(new ConfigurationResponse(), (int)response.StatusCode);
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

                return DefaultUnsuccessfulResponse(new ConfigurationsResponse(), (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                return DefaultExceptionResponse(new ConfigurationsResponse(), "Error getting data from API", ex);
            }
        }

        public async Task<ApiCallResponse<HeaderHistoryResponse>> GetHeaderHistory(string gid, int page, int pageSize)
        {
            try
            {
                var request = new HeaderHistoryRequest { Id = gid, Page = page, PageSize = pageSize };
                using var client = _clientFactory.CreateClient("AdminApi");
                var response = await client.PostAsJsonAsync("Configurations/HeaderHistory", request);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadFromJsonAsync<HeaderHistoryResponse>() ?? new HeaderHistoryResponse();
                    return new ApiCallResponse<HeaderHistoryResponse> { IsSuccess = true, Response = content };
                }
                
                return DefaultUnsuccessfulResponse(new HeaderHistoryResponse(), (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                return DefaultExceptionResponse(new HeaderHistoryResponse(), "Error getting data from API", ex);
            }
        }
        
        public async Task<ApiCallResponse<ConfigurationHistoryResponse>> GetConfigurationHistory(string headerId, string gid, int page, int pageSize)
        {
            try
            {
                var request = new ConfigurationHistoryRequest { HeaderId = headerId, Id = gid, Page = page, PageSize = pageSize };
                using var client = _clientFactory.CreateClient("AdminApi");
                var response = await client.PostAsJsonAsync("Configurations/History", request);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadFromJsonAsync<ConfigurationHistoryResponse>() ?? new ConfigurationHistoryResponse();
                    return new ApiCallResponse<ConfigurationHistoryResponse> { IsSuccess = true, Response = content };
                }
                
                return DefaultUnsuccessfulResponse(new ConfigurationHistoryResponse(), (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                return DefaultExceptionResponse(new ConfigurationHistoryResponse(), "Error getting data from API", ex);
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

                return DefaultUnsuccessfulResponse(new EnvironmentsResponse(), (int)response.StatusCode);
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

                return DefaultUnsuccessfulResponse(new ApplicationsResponse(), (int)response.StatusCode);
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

        public async Task<ApiCallResponse<bool>> DeleteConfiguration(string id, bool permanent)
        {
            try
            {
                using var client = _clientFactory.CreateClient("AdminApi");
                await client.PostAsync($"Configurations/delete/{id}/{permanent}", null);
                return new ApiCallResponse<bool> { IsSuccess = true };
            }
            catch (Exception ex)
            {
                return DefaultExceptionResponse(false, "Error deleting configuration", ex);
            }
        }
    }
}
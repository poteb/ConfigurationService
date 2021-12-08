using System.Net.Http.Json;

namespace Config.Admin.WebClient.Services
{
    public interface IAdminApiService
    {
        Task<List<Model.Environment>> GetEnvironments();
    }

    public class AdminApiService : IAdminApiService
    {
        private readonly HttpClient _client;

        public AdminApiService(HttpClient client)
        {
            _client = client;
        }

        public async Task<List<Model.Environment>> GetEnvironments()
        {
            return (await _client.GetFromJsonAsync<IEnumerable<Model.Environment>>("environments")).ToList();
        }
    }
}
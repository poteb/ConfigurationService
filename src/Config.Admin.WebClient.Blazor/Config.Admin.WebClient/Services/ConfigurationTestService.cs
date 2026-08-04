using pote.Config.Admin.WebClient.Model;
using pote.Config.Shared;

namespace pote.Config.Admin.WebClient.Services;

public interface IConfigurationTestService
{
    Task<List<ParseResponse>> RunTest(Configuration configuration);
}

public class ConfigurationTestService : IConfigurationTestService
{
    private readonly IApiService _apiService;
    private Random _random = new();

    public ConfigurationTestService(IApiService apiService)
    {
        _apiService = apiService;
    }
    
    public async Task<List<ParseResponse>> RunTest(Configuration configuration)
    {
        List<ParseResponse> results = new();
        foreach (var application in configuration.Applications)
        {
            foreach (var environment in configuration.Environments)
            {
                //await Task.Delay(_random.Next(0, 1000));
                var response = await _apiService.TestConfiguration(configuration.Json, application, environment);
                if (!response.IsSuccess || response.Response == null)
                {
                    results.Add(new ParseResponse
                    {
                        Problems = new List<string> { "Call to API failed" }
                    });
                    continue;
                }
                results.Add(response.Response);
            }
        }
        return results;
    }
}
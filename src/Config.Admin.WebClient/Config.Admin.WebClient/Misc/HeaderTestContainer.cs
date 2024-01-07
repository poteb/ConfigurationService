using pote.Config.Admin.WebClient.Model;
using pote.Config.Admin.WebClient.Services;
using pote.Config.Shared;

namespace pote.Config.Admin.WebClient.Misc;

public interface IHeaderTestContainer
{
    TestStages TestStage { get; }
    ConfigurationHeader Header { get; }
    Dictionary<string,List<ParseResponse>> ParseResponses { get; }
    Task RunTests();
    void UpdateHeader(ConfigurationHeader header);
}

public class HeaderTestContainer : IHeaderTestContainer
{
    private readonly IConfigurationTestService _configurationTestService;
    
    public ConfigurationHeader Header { get; private set; }
    public TestStages TestStage { get; private set; } = TestStages.NotStarted;
    public Dictionary<string,List<ParseResponse>> ParseResponses { get; } = new();

    public HeaderTestContainer(ConfigurationHeader header, IConfigurationTestService configurationTestService)
    {
        Header = header;
        _configurationTestService = configurationTestService;
    }
    
    public async Task RunTests()
    {
        ParseResponses.Clear();
        TestStage = TestStages.InProgress;
        foreach (var configuration in Header.Configurations)
        {
            var response = await _configurationTestService.RunTest(configuration);
            if(response.Any(r => r.Problems.Any()))
                TestStage = TestStages.Failed;
            ParseResponses.Add(configuration.Id, response);
        }
        if (TestStage != TestStages.Failed)
            TestStage = TestStages.Complete;
    }

    public void UpdateHeader(ConfigurationHeader header)
    {
        Header = header;
    }
}
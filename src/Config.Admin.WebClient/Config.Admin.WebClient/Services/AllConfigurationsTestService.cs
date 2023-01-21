using pote.Config.Admin.WebClient.Misc;
using pote.Config.Admin.WebClient.Model;

namespace pote.Config.Admin.WebClient.Services;

public enum TestStages
{
    NotStarted,
    InProgress,
    Complete,
    Failed
}

public interface IAllConfigurationsTestService
{
    IHeaderTestContainer CreateAndAddTestContainer(ConfigurationHeader header);
    Task TestAll();
}

public class AllConfigurationsTestService : IAllConfigurationsTestService
{
    private readonly List<IHeaderTestContainer> _testContainers = new();
    private readonly IConfigurationTestService _configurationTestService;

    public AllConfigurationsTestService(IConfigurationTestService configurationTestService)
    {
        _configurationTestService = configurationTestService;
    }

    public IHeaderTestContainer CreateAndAddTestContainer(ConfigurationHeader header)
    {
        var exists = _testContainers.FirstOrDefault(c => c.Header.Id == header.Id);
        if (exists != null)
            return exists;
        var container = new HeaderTestContainer(header, _configurationTestService);
        _testContainers.Add(container);
        return container;
    }

    public async Task TestAll()
    {
        var tasks = new List<Task>();
        foreach (var testContainer in _testContainers)
        {
            tasks.Add(testContainer.RunTests());
        }
        await Task.WhenAll(tasks);
    }
}
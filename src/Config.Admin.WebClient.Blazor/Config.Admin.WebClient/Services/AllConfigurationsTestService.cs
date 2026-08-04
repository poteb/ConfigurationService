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
    void UpdateTestContainer(ConfigurationHeader header);
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

    /// <summary>Creates a new test container if it does not exist, otherwise updates the existing one.</summary>
    public IHeaderTestContainer CreateAndAddTestContainer(ConfigurationHeader header)
    {
        var exists = _testContainers.FirstOrDefault(c => c.Header.Id == header.Id);
        if (exists != null)
        {
            exists.UpdateHeader(header);
            return exists;
        }
        var container = new HeaderTestContainer(header, _configurationTestService);
        _testContainers.Add(container);
        return container;
    }

    public void UpdateTestContainer(ConfigurationHeader header)
    {
        var exists = _testContainers.FirstOrDefault(c => c.Header.Id == header.Id);
        if (exists != null)
            exists.UpdateHeader(header);
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
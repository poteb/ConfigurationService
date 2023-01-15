using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using pote.Config.Admin.Api.Mappers;
using pote.Config.Admin.Api.Services;
using pote.Config.DbModel;

namespace pote.Config.UnitTests;

[TestFixture]
public class DependencyGraphServiceTests
{
    [Test, Explicit]
    public async Task Test_GetReferenceMap()
    {
        var dataProvider = new TestDataProvider();
        var graphService = new DependencyGraphService(dataProvider);
        var headers = new List<ConfigurationHeader> { dataProvider.GetConfiguration("HeaderId1", CancellationToken.None).Result };
        var applications = await dataProvider.GetApplications(CancellationToken.None);
        var environments = await dataProvider.GetEnvironments(CancellationToken.None);
        var apiHeaders = ConfigurationMapper.ToApi(headers, applications, environments);
        var referenceMap = graphService.GetReferenceMap(apiHeaders);
    }
}
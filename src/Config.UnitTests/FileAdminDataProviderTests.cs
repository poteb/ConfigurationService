using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using pote.Config.DataProvider.File;
using pote.Config.Shared;

namespace pote.Config.UnitTests;

[TestFixture]
public class FileAdminDataProviderTests
{
    private const string Environment1 = "{\"Id\":\"650782c9-ae07-43fd-86fc-7d3c4dfa9fc5\",\"Name\":\"Environment 1\"}";
    private const string Environment2 = "{\"Id\":\"75730781-ea22-4a27-8dd5-65178b21721a\",\"Name\":\"Environment 2\"}";
    private const string Environment3 = "{\"Id\":\"a91f10e3-c5b2-43d5-b59e-4f2e02f02d97\",\"Name\":\"Environment 3\"}";
    
    [Test]
    public async Task GetEnvironmentsTest()
    {
        var moq = Mock.Of<IFileHandler>(fh =>
            fh.GetEnvironmentFiles() == new[]{"file1","file2","file3"} &&
            fh.GetEnvironmentContentAbsoluePath("file1", CancellationToken.None) == Task.FromResult(Environment1) &&
            fh.GetEnvironmentContentAbsoluePath("file2", CancellationToken.None) == Task.FromResult(Environment2) &&
            fh.GetEnvironmentContentAbsoluePath("file3", CancellationToken.None) == Task.FromResult(Environment3) 
            );
        
        var applicationMoq = Mock.Of<IApplicationDataAccess>();
        var environmentAccess = new EnvionmentDataAccess(moq);
        var provider = new AdminDataProvider(moq, applicationMoq, environmentAccess);
        var files = await provider.GetEnvironments(CancellationToken.None);

        Assert.AreEqual(3, files.Count);
        Assert.AreEqual("650782c9-ae07-43fd-86fc-7d3c4dfa9fc5", files[0].Id);
        Assert.AreEqual("75730781-ea22-4a27-8dd5-65178b21721a", files[1].Id);
        Assert.AreEqual("a91f10e3-c5b2-43d5-b59e-4f2e02f02d97", files[2].Id);
        Assert.AreEqual("Environment 1", files[0].Name);
        Assert.AreEqual("Environment 2", files[1].Name);
        Assert.AreEqual("Environment 3", files[2].Name);
    }
}
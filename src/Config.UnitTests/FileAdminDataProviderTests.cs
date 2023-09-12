using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using pote.Config.DataProvider.File;
using pote.Config.DataProvider.Interfaces;
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
        var fileHandler = Substitute.For<IFileHandler>();
        fileHandler.GetEnvironmentFiles().Returns(new[] {"file1", "file2", "file3"});
        fileHandler.GetEnvironmentContentAbsolutePath("file1", CancellationToken.None).Returns(Task.FromResult(Environment1));
        fileHandler.GetEnvironmentContentAbsolutePath("file2", CancellationToken.None).Returns(Task.FromResult(Environment2));
        fileHandler.GetEnvironmentContentAbsolutePath("file3", CancellationToken.None).Returns(Task.FromResult(Environment3));

        var applicationDataAccess = Substitute.For<IApplicationDataAccess>();

        var environmentAccess = new EnvironmentDataAccess(fileHandler);
        var provider = new AdminDataProvider(fileHandler, applicationDataAccess, environmentAccess, new EncryptionSettings {JsonEncryptionKey = "detteErEnVildtGodEncryptionKey11"});
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
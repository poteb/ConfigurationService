using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using pote.Config.Admin.Api.Controllers;
using pote.Config.Admin.Api.Helpers;
using pote.Config.Admin.Api.Model;
using pote.Config.DataProvider.Interfaces;

namespace pote.Config.UnitTests;

[TestFixture]
public class ConfigurationsControllerTests
{
    private ConfigurationsController _controller = null!;
    private IAdminDataProvider _dataProvider = null!;

    [SetUp]
    public void Setup()
    {
        var logger = Substitute.For<ILogger<ConfigurationsController>>();
        _dataProvider = Substitute.For<IAdminDataProvider>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var auditLogHandler = Substitute.For<IAuditLogHandler>();
        auditLogHandler.AuditLogConfiguration(default!, default!, default!).ReturnsForAnyArgs(Task.CompletedTask);
        _controller = new ConfigurationsController(logger, _dataProvider, memoryCache, auditLogHandler);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Test]
    public async Task Insert_ValidJson_ReturnsOk()
    {
        _dataProvider.InsertConfiguration(Arg.Any<Config.DbModel.ConfigurationHeader>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var header = new ConfigurationHeader
        {
            Name = "Test",
            Configurations = new List<Configuration>
            {
                new() { Json = """{"key":"value"}""", Applications = "[]", Environments = "[]" }
            }
        };

        var result = await _controller.Insert(header, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkResult>());
    }

    [Test]
    public async Task Insert_InvalidJson_ReturnsBadRequest()
    {
        var header = new ConfigurationHeader
        {
            Name = "Test",
            Configurations = new List<Configuration>
            {
                new() { Json = "{invalid json}" }
            }
        };

        var result = await _controller.Insert(header, CancellationToken.None);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Insert_EmptyJson_ReturnsBadRequest()
    {
        var header = new ConfigurationHeader
        {
            Name = "Test",
            Configurations = new List<Configuration>
            {
                new() { Json = "" }
            }
        };

        var result = await _controller.Insert(header, CancellationToken.None);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Insert_MultipleConfigurations_OneInvalid_ReturnsBadRequest()
    {
        var header = new ConfigurationHeader
        {
            Name = "Test",
            Configurations = new List<Configuration>
            {
                new() { Json = """{"key":"value"}""" },
                new() { Json = "not json" }
            }
        };

        var result = await _controller.Insert(header, CancellationToken.None);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }
}

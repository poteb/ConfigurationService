using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using pote.Config.Admin.Api.Mappers;
using pote.Config.Admin.Api.Model;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.Admin.Api.Services;
using pote.Config.Shared;

namespace pote.Config.Admin.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ConfigurationsController : ControllerBase
{
    private readonly ILogger<ConfigurationsController> _logger;
    private readonly IAdminDataProvider _dataProvider;
    private readonly IMemoryCache _memoryCache;

    public ConfigurationsController(ILogger<ConfigurationsController> logger, IAdminDataProvider dataProvider, IMemoryCache memoryCache)
    {
        _logger = logger;
        _dataProvider = dataProvider;
        _memoryCache = memoryCache;
    }

    [HttpGet]
    public async Task<ActionResult<ConfigurationsResponse>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var applications = await _dataProvider.GetApplications(cancellationToken);
            var environments = await _dataProvider.GetEnvironments(cancellationToken);
            var d = await _dataProvider.GetAll(cancellationToken);
            var response = new ConfigurationsResponse
            {
                Configurations = ConfigurationMapper.ToApi(d, applications, environments)
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configurations");
            return Problem(ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ConfigurationResponse>> Get(string id, CancellationToken cancellationToken)
    {
        try
        {
            var header = await _dataProvider.GetConfiguration(id, cancellationToken);
            if (header == null) return NotFound();
            var applications = await _dataProvider.GetApplications(cancellationToken);
            var environments = await _dataProvider.GetEnvironments(cancellationToken);
            var response = new ConfigurationResponse
            {
                Configuration = ConfigurationMapper.ToApi(header, applications, environments),
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting configuration header, id {id}");
            return Problem(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult> Insert([FromBody] ConfigurationHeader header, CancellationToken cancellationToken)
    {
        try
        {
            await _dataProvider.Insert(ConfigurationMapper.ToDb(header), cancellationToken);
            _memoryCache.Remove(DependencyGraphService.CacheName);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error inserting configuration header, id {header.Id}");
            return Problem(ex.Message);
        }
    }
}
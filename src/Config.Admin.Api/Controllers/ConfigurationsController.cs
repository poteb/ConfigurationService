using Microsoft.AspNetCore.Mvc;
using pote.Config.Admin.Api.Mappers;
using pote.Config.Admin.Api.Model;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.Shared;

namespace pote.Config.Admin.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ConfigurationsController : ControllerBase
{
    private readonly ILogger<ConfigurationsController> _logger;
    private readonly IAdminDataProvider _dataProvider;

    public ConfigurationsController(ILogger<ConfigurationsController> logger, IAdminDataProvider dataProvider)
    {
        _logger = logger;
        _dataProvider = dataProvider;
    }

    [HttpGet]
    public async Task<ActionResult<ConfigurationsResponse>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var systems = await _dataProvider.GetSystems(cancellationToken);
            var environments = await _dataProvider.GetEnvironments(cancellationToken);
            var d = await _dataProvider.GetAll(cancellationToken);
            var response = new ConfigurationsResponse
            {
                Configurations = ConfigurationMapper.ToApi(d, systems, environments)
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configurations");
            return Problem(ex.Message);
        }
    }

    [HttpGet("{gid}")]
    public async Task<ActionResult<ConfigurationResponse>> Get(string gid, CancellationToken cancellationToken)
    {
        try
        {
            var (configuration, history) = await _dataProvider.GetConfiguration(gid, cancellationToken);
            if (configuration == null) return NotFound();
            var systems = await _dataProvider.GetSystems(cancellationToken);
            var environments = await _dataProvider.GetEnvironments(cancellationToken);
            var response = new ConfigurationResponse
            {
                Configuration = ConfigurationMapper.ToApi(configuration, systems, environments),
                History = history.Select(h => ConfigurationMapper.ToApi(h, systems, environments)).ToList()
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting configuration, gid {gid}");
            return Problem(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult> Insert([FromBody] Configuration configuration, CancellationToken cancellationToken)
    {
        try
        {
            await _dataProvider.Insert(ConfigurationMapper.ToDb(configuration), cancellationToken);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error inserting configuration, gid {configuration.Gid}");
            return Problem(ex.Message);
        }
    }
}
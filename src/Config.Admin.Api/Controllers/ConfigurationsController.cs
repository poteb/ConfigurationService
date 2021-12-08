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
            var d = await _dataProvider.GetAll(cancellationToken);
            var response = new ConfigurationsResponse
            {
                Configurations = ConfigurationMapper.ToApi(d)
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
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
            var response = new ConfigurationResponse
            {
                Configuration = Mappers.ConfigurationMapper.ToApi(configuration),
                History = history.Select(Mappers.ConfigurationMapper.ToApi).ToList()
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult> Insert([FromBody] Configuration configuration, CancellationToken cancellationToken)
    {
        try
        {
            await _dataProvider.Insert(Mappers.ConfigurationMapper.ToDb(configuration), cancellationToken);
            return Ok();
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }
}
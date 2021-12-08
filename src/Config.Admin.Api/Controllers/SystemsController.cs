using Microsoft.AspNetCore.Mvc;
using pote.Config.Admin.Api.Mappers;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.Shared;

namespace pote.Config.Admin.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SystemsController : ControllerBase
{
    private readonly ILogger<SystemsController> _logger;
    private readonly IAdminDataProvider _dataProvider;

    public SystemsController(ILogger<SystemsController> logger, IAdminDataProvider dataProvider)
    {
        _logger = logger;
        _dataProvider = dataProvider;
    }

    [HttpGet]
    public async Task<ActionResult<SystemsResponse>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var response = new SystemsResponse
            {
                Systems = SystemMapper.ToApi(await _dataProvider.GetSystems(cancellationToken))
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult> Insert([FromBody] Model.System system, CancellationToken cancellationToken)
    {
        try
        {
            await _dataProvider.UpsertSystem(SystemMapper.ToDb(system), cancellationToken);
            return Ok();
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }
}
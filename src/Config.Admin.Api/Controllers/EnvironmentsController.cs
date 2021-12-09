using Microsoft.AspNetCore.Mvc;
using pote.Config.Admin.Api.Mappers;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.Shared;

namespace pote.Config.Admin.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class EnvironmentsController : ControllerBase
{
    private readonly ILogger<EnvironmentsController> _logger;
    private readonly IAdminDataProvider _dataProvider;

    public EnvironmentsController(ILogger<EnvironmentsController> logger, IAdminDataProvider dataProvider)
    {
        _logger = logger;
        _dataProvider = dataProvider;
    }

    [HttpGet]
    public async Task<ActionResult<EnvironmentsResponse>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var response = new EnvironmentsResponse
            {
                Environments = EnvironmentMapper.ToApi(await _dataProvider.GetEnvironments(cancellationToken))
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult> Insert([FromBody] Model.Environment environment, CancellationToken cancellationToken)
    {
        try
        {
            await _dataProvider.UpsertEnvironment(EnvironmentMapper.ToDb(environment), cancellationToken);
            return Ok();
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [HttpDelete]
    public async Task<ActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        try
        {
            await _dataProvider.DeleteEnvironment(id, cancellationToken);
            return Ok();
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }
}
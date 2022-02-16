using Microsoft.AspNetCore.Mvc;
using pote.Config.Admin.Api.Mappers;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.Shared;

namespace pote.Config.Admin.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ApplicationsController : ControllerBase
{
    private readonly ILogger<ApplicationsController> _logger;
    private readonly IAdminDataProvider _dataProvider;

    public ApplicationsController(ILogger<ApplicationsController> logger, IAdminDataProvider dataProvider)
    {
        _logger = logger;
        _dataProvider = dataProvider;
    }

    [HttpGet]
    public async Task<ActionResult<ApplicationsResponse>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var response = new ApplicationsResponse
            {
                Applications = ApplicationMapper.ToApi(await _dataProvider.GetApplications(cancellationToken))
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult> Insert([FromBody] Model.Application application, CancellationToken cancellationToken)
    {
        try
        {
            await _dataProvider.UpsertApplication(ApplicationMapper.ToDb(application), cancellationToken);
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
            await _dataProvider.DeleteApplication(id, cancellationToken);
            return Ok();
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }
}
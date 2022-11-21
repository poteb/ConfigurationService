using Microsoft.AspNetCore.Mvc;
using pote.Config.Admin.Api.Services;

namespace pote.Config.Admin.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class DependencyGraphController : ControllerBase
{
    private readonly ILogger<DependencyGraphController> _logger;
    private readonly IDependencyGraphService _dependencyGraphService;

    public DependencyGraphController(ILogger<DependencyGraphController> logger, IDependencyGraphService dependencyGraphService)
    {
        _logger = logger;
        _dependencyGraphService = dependencyGraphService;
    }
    
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _dependencyGraphService.GetDependencyGraphAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dependency graph");
            return Problem(ex.Message);
        }
    }
}
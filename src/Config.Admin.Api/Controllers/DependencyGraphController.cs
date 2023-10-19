using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.Admin.Api.Services;
using pote.Config.Auth;

namespace pote.Config.Admin.Api.Controllers;

[ApiController]
[Route("[controller]")]
[ApiKey]
public class DependencyGraphController : ControllerBase
{
    private readonly ILogger<DependencyGraphController> _logger;
    private readonly IDependencyGraphService _dependencyGraphService;
    private readonly IMemoryCache _memoryCache;

    public DependencyGraphController(ILogger<DependencyGraphController> logger, IDependencyGraphService dependencyGraphService, IMemoryCache memoryCache)
    {
        _logger = logger;
        _dependencyGraphService = dependencyGraphService;
        _memoryCache = memoryCache;
    }
    
    [HttpGet]
    public async Task<ActionResult<DependencyGraphResponse>> Get(CancellationToken cancellationToken)
    {
        try
        {
            var fromCache = _memoryCache.Get<DependencyGraphResponse>(DependencyGraphService.CacheName);
            if (fromCache != null)
                return Ok(fromCache);
            
            var response = await _dependencyGraphService.GetDependencyGraphAsync(cancellationToken);
            _memoryCache.Set(DependencyGraphService.CacheName, response, TimeSpan.FromDays(1));
            // var options = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles };
            // var jsonResponse = JsonSerializer.Serialize(response, options);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dependency graph");
            return Problem(ex.Message);
        }
    }
}
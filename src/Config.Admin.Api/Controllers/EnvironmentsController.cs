using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using pote.Config.Admin.Api.Helpers;
using pote.Config.Admin.Api.Mappers;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.Admin.Api.Services;
using pote.Config.DataProvider.Interfaces;
using pote.Config.Shared;

namespace pote.Config.Admin.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class EnvironmentsController : ControllerBase
{
    private readonly ILogger<EnvironmentsController> _logger;
    private readonly IAdminDataProvider _dataProvider;
    private readonly IMemoryCache _memoryCache;
    private readonly IAuditLogHandler _auditLogHandler;

    public EnvironmentsController(ILogger<EnvironmentsController> logger, IAdminDataProvider dataProvider, IMemoryCache memoryCache, IAuditLogHandler auditLogHandler)
    {
        _logger = logger;
        _dataProvider = dataProvider;
        _memoryCache = memoryCache;
        _auditLogHandler = auditLogHandler;
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
            _logger.LogError(ex, "Failed to get environments");
            return Problem(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult> Insert([FromBody] Model.Environment environment, CancellationToken cancellationToken)
    {
        try
        {
            await _dataProvider.UpsertEnvironment(EnvironmentMapper.ToDb(environment), cancellationToken);
            _memoryCache.Remove(DependencyGraphService.CacheName);
            await this.AuditLog(environment.Id, "Insert", _auditLogHandler.AuditLogEnvironment);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert environment");
            return Problem(ex.Message);
        }
    }

    [HttpDelete]
    public async Task<ActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        try
        {
            await _dataProvider.DeleteEnvironment(id, cancellationToken);
            _memoryCache.Remove(DependencyGraphService.CacheName);
            await this.AuditLog(id, "Delete", _auditLogHandler.AuditLogEnvironment);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete environment");
            return Problem(ex.Message);
        }
    }
}
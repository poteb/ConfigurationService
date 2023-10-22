using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using pote.Config.Admin.Api.Helpers;
using pote.Config.Admin.Api.Mappers;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.Admin.Api.Services;
using pote.Config.Auth;
using pote.Config.DataProvider.Interfaces;

namespace pote.Config.Admin.Api.Controllers;

[ApiController]
[Route("[controller]")]
[ApiKey]
public class ApplicationsController : ControllerBase
{
    private readonly ILogger<ApplicationsController> _logger;
    private readonly IAdminDataProvider _dataProvider;
    private readonly IMemoryCache _memoryCache;
    private readonly IAuditLogHandler _auditLogHandler;

    public ApplicationsController(ILogger<ApplicationsController> logger, IAdminDataProvider dataProvider, IMemoryCache memoryCache, IAuditLogHandler auditLogHandler)
    {
        _logger = logger;
        _dataProvider = dataProvider;
        _memoryCache = memoryCache;
        _auditLogHandler = auditLogHandler;
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
            _logger.LogError(ex, "Failed to get applications");
            return Problem(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult> Insert([FromBody] Model.Application application, CancellationToken cancellationToken)
    {
        try
        {
            await _dataProvider.UpsertApplication(ApplicationMapper.ToDb(application), cancellationToken);
            _memoryCache.Remove(DependencyGraphService.CacheName);
            await this.AuditLog(application.Id, "Insert", _auditLogHandler.AuditLogApplication);
            _logger.LogInformation("Application {ApplicationId} inserted", application.Id);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert application");
            return Problem(ex.Message);
        }
    }

    [HttpDelete]
    public async Task<ActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        try
        {
            await _dataProvider.DeleteApplication(id, cancellationToken);
            _memoryCache.Remove(DependencyGraphService.CacheName);
            await this.AuditLog(id, "Delete", _auditLogHandler.AuditLogApplication);
            _logger.LogInformation("Application {ApplicationId} deleted", id);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete application");
            return Problem(ex.Message);
        }
    }
}
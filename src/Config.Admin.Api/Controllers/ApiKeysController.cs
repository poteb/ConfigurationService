using Microsoft.AspNetCore.Mvc;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.Auth;
using pote.Config.DataProvider.Interfaces;
using pote.Config.Admin.Api.Helpers;

namespace pote.Config.Admin.Api.Controllers;

[ApiController]
[Route("[controller]")]
[ApiKey]
public class ApiKeysController : ControllerBase
{
    private readonly ILogger<ApiKeysController> _logger;
    private readonly IAdminDataProvider _dataProvider;
    private readonly IAuditLogHandler _auditLogHandler;
    
    public ApiKeysController(ILogger<ApiKeysController> logger, IAdminDataProvider dataProvider, IAuditLogHandler auditLogHandler)
    {
        _logger = logger;
        _dataProvider = dataProvider;
        _auditLogHandler = auditLogHandler;
    }
    
    [HttpGet]
    public async Task<ActionResult<ApiKeysResponse>> Get(CancellationToken cancellationToken)
    {
        try
        {
            var apiKeys = await _dataProvider.GetApiKeys(cancellationToken);
            return Ok(new ApiKeysResponse {ApiKeys = Mappers.ApiKeysMapper.ToApi(apiKeys)});
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get API keys");
            return Problem(ex.Message);
        }
    }
    
    [HttpPost]
    public async Task<ActionResult> Save([FromBody] Model.ApiKeys apiKeys, CancellationToken cancellationToken)
    {
        try
        {
            await _dataProvider.SaveApiKeys(Mappers.ApiKeysMapper.ToDb(apiKeys), cancellationToken);
            await this.AuditLog("0", "Save", _auditLogHandler.AuditLogApiKeys);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            return Problem(ex.Message);
        }
    }
}
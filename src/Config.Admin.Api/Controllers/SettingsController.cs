using Microsoft.AspNetCore.Mvc;
using pote.Config.Admin.Api.Helpers;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.DataProvider.Interfaces;

namespace pote.Config.Admin.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SettingsController : ControllerBase
{
    private readonly ILogger<SettingsController> _logger;
    private readonly IAdminDataProvider _dataProvider;
    private readonly IAuditLogHandler _auditLogHandler;

    public SettingsController(ILogger<SettingsController> logger, IAdminDataProvider dataProvider, IAuditLogHandler auditLogHandler)
    {
        _logger = logger;
        _dataProvider = dataProvider;
        _auditLogHandler = auditLogHandler;
    }

    [HttpGet]
    public async Task<ActionResult<SettingsResponse>> Get(CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _dataProvider.GetSettings(cancellationToken);
            return Ok(new SettingsResponse {Settings = Mappers.SettingsMapper.ToApi(settings)});
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get settings");
            return Problem(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult> Save([FromBody] Model.Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            await _dataProvider.SaveSettings(Mappers.SettingsMapper.ToDb(settings), cancellationToken);
            await this.AuditLog("0", "Save", _auditLogHandler.AuditLogSettings);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            return Problem(ex.Message);
        }
    }
}
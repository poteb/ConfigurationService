using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using pote.Config.Admin.Api.Helpers;
using pote.Config.Admin.Api.Mappers;
using pote.Config.Admin.Api.Model;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.Admin.Api.Services;
using pote.Config.Auth;
using pote.Config.DataProvider.Interfaces;

namespace pote.Config.Admin.Api.Controllers;

[ApiController]
[Route("[controller]")]
[ApiKey]
public class ConfigurationsController : ControllerBase
{
    private readonly ILogger<ConfigurationsController> _logger;
    private readonly IAdminDataProvider _dataProvider;
    private readonly IMemoryCache _memoryCache;
    private readonly IAuditLogHandler _auditLogHandler;

    public ConfigurationsController(ILogger<ConfigurationsController> logger, IAdminDataProvider dataProvider, IMemoryCache memoryCache, IAuditLogHandler auditLogHandler)
    {
        _logger = logger;
        _dataProvider = dataProvider;
        _memoryCache = memoryCache;
        _auditLogHandler = auditLogHandler;
    }

    [HttpGet]
    public async Task<ActionResult<ConfigurationsResponse>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var applications = await _dataProvider.GetApplications(cancellationToken);
            var environments = await _dataProvider.GetEnvironments(cancellationToken);
            var d = await _dataProvider.GetAll(cancellationToken);
            var response = new ConfigurationsResponse
            {
                Configurations = ConfigurationMapper.ToApi(d, applications, environments)
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configurations");
            return Problem(ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ConfigurationResponse>> Get(string id, CancellationToken cancellationToken)
    {
        try
        {
            var header = await _dataProvider.GetConfiguration(id, cancellationToken);
            var applications = await _dataProvider.GetApplications(cancellationToken);
            var environments = await _dataProvider.GetEnvironments(cancellationToken);
            var response = new ConfigurationResponse
            {
                Configuration = ConfigurationMapper.ToApi(header, applications, environments),
            };
            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration header, id {Id}", id);
            return Problem(ex.Message);
        }
    }

    [HttpPost("headerhistory")]
    public async Task<ActionResult<HeaderHistoryResponse>> GetHistory(HeaderHistoryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var history = await _dataProvider.GetHeaderHistory(request.Id, request.Page, request.PageSize, cancellationToken);
            var applications = await _dataProvider.GetApplications(cancellationToken);
            var environments = await _dataProvider.GetEnvironments(cancellationToken);
            var apiHistory = ConfigurationMapper.ToApi(history, applications, environments);
            var response = new HeaderHistoryResponse { History = apiHistory, Page = request.Page, PageSize = request.PageSize };
            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration history, id {Id}, page {Page}, pageSize {PageSize}", request.Id, request.Page, request.PageSize);
            return Problem(ex.Message);
        }
    }

    [HttpPost("history")]
    public async Task<ActionResult<ConfigurationHistoryResponse>> GetHistory(ConfigurationHistoryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var history = await _dataProvider.GetConfigurationHistory(request.HeaderId, request.Id, request.Page, request.PageSize, cancellationToken);
            var applications = await _dataProvider.GetApplications(cancellationToken);
            var environments = await _dataProvider.GetEnvironments(cancellationToken);
            var apiHistory = ConfigurationMapper.ToApi(history, applications, environments);
            var response = new ConfigurationHistoryResponse { History = apiHistory, Page = request.Page, PageSize = request.PageSize };
            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration history, id {Id}, page {Page}, pageSize {PageSize}", request.Id, request.Page, request.PageSize);
            return Problem(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult> Insert([FromBody] ConfigurationHeader header, CancellationToken cancellationToken)
    {
        try
        {
            await _dataProvider.Insert(ConfigurationMapper.ToDb(header), cancellationToken);
            _memoryCache.Remove(DependencyGraphService.CacheName);
            await this.AuditLog(header.Id, "Insert", _auditLogHandler.AuditLogConfiguration);
            _logger.LogInformation("Configuration header {HeaderId} inserted", header.Id);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting configuration header, id {HeaderId}", header.Id);
            return Problem(ex.Message);
        }
    }

    [HttpPost("delete/{id}/{permanent}")]
    public async Task<ActionResult> Delete(string id, bool permanent = false)
    {
        try
        {
            _dataProvider.DeleteConfiguration(id, permanent);
            if (!permanent)
                await this.AuditLog(id, "Delete", _auditLogHandler.AuditLogConfiguration);
            _logger.LogInformation("Configuration header {HeaderId} deleted", id);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration header, id {Id}", id);
            return Problem(ex.Message);
        }
    }
}
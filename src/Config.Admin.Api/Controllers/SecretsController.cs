using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using pote.Config.Admin.Api.Helpers;
using pote.Config.Admin.Api.Mappers;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.Admin.Api.Services;
using pote.Config.Auth;
using pote.Config.DataProvider.Interfaces;
using pote.Config.Shared;

namespace pote.Config.Admin.Api.Controllers;

[ApiController]
[Route("[controller]")]
[ApiKey]
public class SecretsController : ControllerBase
{
    private readonly ILogger<SecretsController> _logger;
    private readonly IAdminDataProvider _dataProvider;
    private readonly IMemoryCache _memoryCache;
    private readonly IAuditLogHandler _auditLogHandler;

    public SecretsController(ILogger<SecretsController> logger, IAdminDataProvider dataProvider, IMemoryCache memoryCache, IAuditLogHandler auditLogHandler)
    {
        _logger = logger;
        _dataProvider = dataProvider;
        _memoryCache = memoryCache;
        _auditLogHandler = auditLogHandler;
    }

    [HttpGet]
    public async Task<ActionResult<SecretsResponse>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var applications = await _dataProvider.GetApplications(cancellationToken);
            var environments = await _dataProvider.GetEnvironments(cancellationToken);
            var d = await _dataProvider.GetAllSecretHeaders(cancellationToken);
            var response = new SecretsResponse
            {
                Secrets = SecretMapper.ToApi(d, applications, environments)
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting secrets");
            return Problem(ex.Message);
        }
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<SecretResponse>> Get(string id, CancellationToken cancellationToken)
    {
        try
        {
            var header = await _dataProvider.GetSecret(id, cancellationToken);
            var applications = await _dataProvider.GetApplications(cancellationToken);
            var environments = await _dataProvider.GetEnvironments(cancellationToken);
            var response = new SecretResponse
            {
                Secret = SecretMapper.ToApi(header, applications, environments),
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

    [HttpPost]
    public async Task<ActionResult> Insert([FromBody] Model.SecretHeader secret, CancellationToken cancellationToken)
    {
        try
        {
            await _dataProvider.InsertSecret(SecretMapper.ToDb(secret), cancellationToken);
            _memoryCache.Remove(DependencyGraphService.CacheName);
            await this.AuditLog(secret.Id, "Insert", _auditLogHandler.AuditLogSecrets);
            _logger.LogInformation("Secret {SecretId} inserted", secret.Id);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting sercret header, id {SecretId}", secret.Id);
            return Problem(ex.Message);
        }
    }

    [HttpDelete]
    public async Task<ActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        try
        {
            await _dataProvider.DeleteSecret(id, cancellationToken);
            _memoryCache.Remove(DependencyGraphService.CacheName);
            await this.AuditLog(id, "Delete", _auditLogHandler.AuditLogSecrets);
            _logger.LogInformation("Secret {SecretId} deleted", id);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete secret");
            return Problem(ex.Message);
        }
    }
}
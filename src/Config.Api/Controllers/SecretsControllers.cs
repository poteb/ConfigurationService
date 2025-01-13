using Microsoft.AspNetCore.Mvc;
using pote.Config.Auth;
using pote.Config.DataProvider.Interfaces;
using pote.Config.Shared.Secrets;

namespace pote.Config.Api.Controllers;

[ApiController]
[Route("Secrets")]
[ApiKey]
public class SecretsControllers : ControllerBase
{
    private readonly ILogger<SecretsControllers> _logger;
    private readonly IDataProvider _dataProvider;

    public SecretsControllers(ILogger<SecretsControllers> logger, IDataProvider dataProvider)
    {
        _logger = logger;
        _dataProvider = dataProvider;
    }
    [HttpPost]
    public async Task<ActionResult<SecretValueResponse>> Get(SecretValueRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var application = await _dataProvider.GetApplication(request.Application, cancellationToken);
            var environment = await _dataProvider.GetEnvironment(request.Environment, cancellationToken);
            
            var result = await _dataProvider.GetSecretValue(request.SecretName, application.Id, environment.Id, cancellationToken);
            var response = new SecretValueResponse
            {
                Value = result
            };
            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting secret value, name {Name}", request.SecretName);
            return Problem(ex.Message);
        }
    }
}
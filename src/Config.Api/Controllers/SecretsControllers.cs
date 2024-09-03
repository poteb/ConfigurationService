using Microsoft.AspNetCore.Mvc;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.Auth;
using pote.Config.DataProvider.Interfaces;

namespace pote.Config.Api.Controllers;

[ApiController]
[Route("Secrets")]
[ApiKey]
public class SecretsControllers : ControllerBase
{
    private readonly ILogger<SecretsControllers> _logger;
    private readonly EncryptionSettings _encryptionSettings;
    private readonly IDataProvider _dataProvider;

    public SecretsControllers(ILogger<SecretsControllers> logger, IDataProvider dataProvider, EncryptionSettings encryptionSettings)
    {
        _logger = logger;
        _encryptionSettings = encryptionSettings;
        _dataProvider = dataProvider;
    }
    [HttpPost]
    public async Task<ActionResult<SecretValueResponse>> Get(SecretValueRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _dataProvider.GetSecretValue(request.SecretName, request.Application, request.Environment, cancellationToken);
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
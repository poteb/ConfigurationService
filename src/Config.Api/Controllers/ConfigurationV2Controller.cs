using Microsoft.AspNetCore.Mvc;
using pote.Config.Auth;

namespace pote.Config.Api.Controllers;

[ApiController]
[Route("V2/Configuration")]
[ApiKey]
public class ConfigurationV2Controller : ControllerBase
{
    private readonly ILogger<ConfigurationV2Controller> _logger;
    private readonly IParser _parser;
    private readonly EncryptionSettings _encryptionSettings;
    
    public ConfigurationV2Controller(ILogger<ConfigurationV2Controller> logger, IParser parser, EncryptionSettings encryptionSettings)
    {
        _logger = logger;
        _parser = parser;
        _encryptionSettings = encryptionSettings;
    }
    
    [HttpGet]
    public ActionResult Get()
    {
        _logger.LogInformation("Method {Controller}.{Get}", nameof(ConfigurationV2Controller), nameof(Parse));
        return Ok("Wagga");
    }

    [HttpPost]
    public async Task<ActionResult> Parse(ParseRequest request)
    {
        try
        {
            _logger.LogInformation("Method {Controller}.{Method} called from application {Application}", nameof(ConfigurationV2Controller), nameof(Parse), request.Application);
            var response = new ParseResponse { Application = request.Application, Environment = request.Environment };
            var config = await _parser.Parse(request.AsJson(), request.Application, request.Environment, response.AddProblem, CancellationToken.None, _encryptionSettings.JsonEncryptionKey);
            response.FromJson(config);
            return Ok(response);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error parsing request.");
            return Problem($"Error parsing request: {e.Message}");
        }
    }
}
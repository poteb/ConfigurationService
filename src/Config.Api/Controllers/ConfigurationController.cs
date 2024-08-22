using Microsoft.AspNetCore.Mvc;

namespace pote.Config.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly ILogger<ConfigurationController> _logger;
    private readonly IParser _parser;
    private readonly EncryptionSettings _encryptionSettings;

    public ConfigurationController(ILogger<ConfigurationController> logger, IParser parser, EncryptionSettings encryptionSettings)
    {
        _logger = logger;
        _parser = parser;
        _encryptionSettings = encryptionSettings;
    }

    [HttpGet]
    public ActionResult Get()
    {
        _logger.LogInformation(nameof(Get));
        return Ok("Wagga");
    }

    [HttpPost]
    public async Task<ActionResult> Parse([FromBody] ParseRequest request)
    {
        try
        {
            if (!request.Application.Equals("ApiKeys"))
                _logger.LogInformation("Method {Controller}{Method} called from application {Application}", nameof(ConfigurationController), nameof(Parse), request.Application);
            var response = new ParseResponse { Application = request.Application, Environment = request.Environment };
            var config = await _parser.Parse(request.AsJson(), request.Application, request.Environment, response.AddProblem, CancellationToken.None, _encryptionSettings.JsonEncryptionKey, request.ResolveSecrets);
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
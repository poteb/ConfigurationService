using Microsoft.AspNetCore.Mvc;
using pote.Config.Api.Authentication;

namespace pote.Config.Api.Controllers;

[ApiController]
[Route("V2/[controller]")]
[ApiKey]
public class ConfigurationV2Controller : ConfigurationController
{
    public ConfigurationV2Controller(ILogger<ConfigurationController> logger, IParser parser, EncryptionSettings encryptionSettings) : base(logger, parser, encryptionSettings)
    {
    }
}

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
        return Ok("Wagga");
    }

    [HttpPost]
    public async Task<ActionResult> Parse([FromBody] ParseRequest request)
    {
        try
        {
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
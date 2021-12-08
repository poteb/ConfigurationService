using Microsoft.AspNetCore.Mvc;

namespace pote.Config.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ConfigController : ControllerBase
{
    private readonly ILogger<ConfigController> _logger;
    private readonly IParser _parser;

    public ConfigController(ILogger<ConfigController> logger, IParser parser)
    {
        _logger = logger;
        _parser = parser;
    }

    [HttpPost("parse")]
    public async Task<ActionResult> Parse([FromBody] ParseRequest request)
    {
        try
        {
            var response = new ParseResponse { System = request.System, Environment = request.Environment };
            var config = await _parser.Parse(request.AsJson(), request.System, request.Environment, response.AddProblem, CancellationToken.None);
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
using Microsoft.AspNetCore.Mvc;

namespace pote.Config.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly ILogger<ConfigurationController> _logger;
    private readonly IParser _parser;

    public ConfigurationController(ILogger<ConfigurationController> logger, IParser parser)
    {
        _logger = logger;
        _parser = parser;
    }

    [HttpPost]
    public async Task<ActionResult> Parse([FromBody] ParseRequest request)
    {
        try
        {
            var response = new ParseResponse { Application = request.Application, Environment = request.Environment };
            var config = await _parser.Parse(request.AsJson(), request.Application, request.Environment, response.AddProblem, CancellationToken.None);
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
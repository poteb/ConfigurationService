using Microsoft.AspNetCore.Mvc;
using pote.Config.Shared;

namespace pote.Config.Admin.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ConfigParseController : ControllerBase
{
    private readonly ILogger<ConfigParseController> _logger;
    private readonly IParser _parser;

    public ConfigParseController(ILogger<ConfigParseController> logger, IParser parser)
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
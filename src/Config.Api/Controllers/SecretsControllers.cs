// using Microsoft.AspNetCore.Mvc;
// using pote.Config.Auth;
//
// namespace pote.Config.Api.Controllers;
//
// [ApiController]
// [Route("Secrets")]
// //[ApiKey]
// public class SecretsControllers
// {
//     private readonly ILogger<SecretsControllers> _logger;
//     private readonly IParser _parser;
//     private readonly EncryptionSettings _encryptionSettings;
//
//     public SecretsControllers(ILogger<SecretsControllers> logger, IParser parser, EncryptionSettings encryptionSettings)
//     {
//         _logger = logger;
//         _parser = parser;
//         _encryptionSettings = encryptionSettings;
//     }
//     [HttpGet]
//     public ActionResult Get([FromQuery] string secret)
//     {
//         try
//         {
//             _logger.LogInformation("Method {Controller}.{Method} called from application {Application}", nameof(ConfigurationV2Controller), nameof(Parse), request.Application);
//             var response = new ParseResponse { Application = request.Application, Environment = request.Environment };
//             var config = await _parser.Parse(request.AsJson(), request.Application, request.Environment, response.AddProblem, CancellationToken.None, _encryptionSettings.JsonEncryptionKey);
//             response.FromJson(config);
//             return Ok(response);
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, "Error parsing request.");
//             return Problem($"Error parsing request: {e.Message}");
//         }
//     }
// }
using Microsoft.AspNetCore.Mvc;
using pote.Config.Shared;

namespace pote.Config.Middleware.TestApi.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly IConfiguration _configuration;
    private readonly ISecretResolver _secretResolver;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, IConfiguration configuration, ISecretResolver secretResolver)
    {
        _logger = logger;
        _configuration = configuration;
        _secretResolver = secretResolver;
        var d = configuration.GetSection("SecretSettings").Get<SecretSettings>();
        var dd = secretResolver.ResolveSecret(d.Secret1).Result;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        
        
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }
}
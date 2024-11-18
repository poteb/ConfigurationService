using Microsoft.AspNetCore.Mvc;

namespace pote.Config.Middleware.TestApi.Controllers;

[ApiController]
[Route("[controller]")]
public class SecretTestController : ControllerBase
{
    private readonly MySecrets _secrets;

    public SecretTestController(MySecrets secrets)
    {
        _secrets = secrets;
    }

    [HttpGet("GetSecretSync")]
    public string GetSecretSync()
    {
        return _secrets.Secret1;
    }
    
    [HttpGet("GetSecretAsync")]
    public async Task<string> GetSecretAsync()
    {
        return await Task.FromResult(_secrets.Secret1);
    }
}
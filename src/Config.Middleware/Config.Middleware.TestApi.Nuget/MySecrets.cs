using pote.Config.Shared.Secrets;

namespace Config.Middleware.TestApi.Nuget;

public partial class MySecrets
{
    [Secret] private string _secret;
}
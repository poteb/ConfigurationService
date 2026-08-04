using System.Security.Cryptography;

namespace pote.Config.Admin.Api.Auth;

public static class TokenGenerator
{
    /// <summary>256-bit random token, url-safe base64 without padding.</summary>
    public static string NewToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}

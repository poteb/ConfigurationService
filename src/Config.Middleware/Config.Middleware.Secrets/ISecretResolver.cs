using System.Threading.Tasks;

namespace pote.Config.Middleware.Secrets;

public interface ISecretResolver
{
    string ResolveSecret(string secret);
    Task<string> ResolveSecretAsync(string secret);
}
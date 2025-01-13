namespace pote.Config.Shared.Secrets;

/// <summary>Resolves secrets from the configuration API</summary>
public interface ISecretResolver
{
    /// <summary>Resolves a secret from the configuration API</summary>
    /// <param name="secret">The secret to resolve</param>
    /// <returns>The resolved secret</returns>
    /// <exception cref="Exception">Thrown when an error occurs while resolving the secret</exception>
    string ResolveSecret(string secret);
    
    /// <summary>Resolves the secret asynchronously.</summary>
    /// <param name="secret">The secret to resolve.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the resolved secret value.</returns>
    /// <exception cref="Exception">Thrown when the API request fails.</exception>
    Task<string> ResolveSecretAsync(string secret);
}
using Microsoft.Extensions.DependencyInjection;
using pote.Config.Shared.Secrets;

namespace pote.Config.Middleware;

public static class SecretsExtensionMethods
{
    /// <summary>
    /// Creates a SecretResolver and adds it to the DI container.
    /// The SecretResolver has to be created manually because it needs to be added to the options, which are loaded from appsettings.
    /// There is no way that I know of that can add options to DI because JSON is converted to an object and then added to DI. The options can't have a constructor that takes a parameter.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the SecretResolver to.</param>
    /// <param name="configuration">The BuilderConfiguration that is used to get the application, environment and API URI.</param>
    /// <param name="apiKey">The API key that is used to authenticate with the API.</param>
    /// <returns>Returns the SecretResolver so that it can be added to the options when calling AddSecretConfiguration.</returns>
    public static ISecretResolver AddSecretsResolver(this IServiceCollection services, BuilderConfiguration configuration, string apiKey)
    {
        var client = CreateHttpClient(apiKey);
        var secretResolver = new SecretResolver(configuration, () => client);
        services.AddSingleton(secretResolver);
        return secretResolver;
    }
    
    private static HttpClient CreateHttpClient(string apiKey)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        return client;
    }
}
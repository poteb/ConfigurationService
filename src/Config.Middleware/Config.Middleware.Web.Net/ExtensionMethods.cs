using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using pote.Config.Shared;

namespace pote.Config.Middleware;

public static class ExtensionMethods
{
    public static async Task<WebApplicationBuilder> AddConfigurationFromApi(this WebApplicationBuilder builder, BuilderConfiguration configuration, string inputJson, Func<HttpClient> clientProvider, Action<string, Exception> errorOutput = null!)
    {
        var parsedJsonFile = Path.Combine(configuration.WorkingDirectory, $"appsettings.{configuration.Environment}.Parsed.json");
        try
        {
            var apiCommunication = new ApiCommunication(configuration.ApiUri, clientProvider);
            var response = await apiCommunication.GetConfiguration(new ParseRequest(configuration.Application, configuration.Environment, inputJson));
            if (response == null)  throw new InvalidDataException("Response from API was empty.");
            var json = response.GetJson();
            await File.WriteAllTextAsync(parsedJsonFile, json);
            builder.Configuration.AddJsonFile(parsedJsonFile, false, false);
            return builder;
        }
        catch (Exception ex)
        {
            errorOutput("Error getting configuration from API. Loading previously parsed configuration.", ex);
            return builder.AddPreviouslyParsedConfiguration(parsedJsonFile);
        }
    }
    
    private static WebApplicationBuilder AddPreviouslyParsedConfiguration(this WebApplicationBuilder builder, string file)
    {
        if (!File.Exists(file))
            throw new FileNotFoundException($"An old parsed configuration not found, file: {file}");
        builder.Configuration.AddJsonFile(file, false, true);
        return builder;
    }
    
    public static IServiceCollection AddSecretsResolver(this IServiceCollection services, Func<HttpClient> clientProvider, BuilderConfiguration configuration)
    {
        var secretResolver = new SecretResolver(configuration, clientProvider);
        services.AddSingleton<ISecretResolver>(secretResolver);
        return services;
    }
}
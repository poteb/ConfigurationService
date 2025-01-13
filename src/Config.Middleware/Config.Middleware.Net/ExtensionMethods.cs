using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using pote.Config.Shared;

namespace pote.Config.Middleware;

public static class ExtensionMethods
{
    public static async Task<IConfigurationBuilder> AddConfigurationFromApi(this IConfigurationBuilder builder, BuilderConfiguration configuration, string inputJson, Func<HttpClient> clientProvider, Action<string, Exception> errorOutput = null!)
    {
        var parsedJsonFile = Path.Combine(configuration.WorkingDirectory, $"appsettings.{configuration.Environment}.Parsed.json");
        try
        {
            var apiCommunication = new ApiCommunication(configuration.RootApiUri, clientProvider.Invoke());
            var response = await apiCommunication.GetConfiguration(new ParseRequest(configuration.Application, configuration.Environment, inputJson));
            if (response == null)  throw new InvalidDataException("Reponse from API was empty.");
            var json = response.GetJson();
            await File.WriteAllTextAsync(parsedJsonFile, json);
            return builder.AddJsonFile(parsedJsonFile, false, false);
        }
        catch (Exception ex)
        {
            errorOutput("Error getting configuration from API. Loading previously parsed configuration.", ex);
            return builder.AddPreviouslyParsedConfiguration(parsedJsonFile);
        }
    }
    
    private static IConfigurationBuilder AddPreviouslyParsedConfiguration(this IConfigurationBuilder builder, string file)
    {
        if (!File.Exists(file))
            throw new FileNotFoundException($"An old parsed configuration not found, file: {file}");
        return builder.AddJsonFile(file, false, true);
    }
    
    /// <summary>Adds the BuilderConfiguration to the DI container.</summary>
    /// <param name="services">The IServiceCollection to add the BuilderConfiguration to.</param>
    /// <param name="application">The name of the application that is being configured.</param>
    /// <param name="environment">The environment that the application is running in.</param>
    /// <param name="apiUri">The URI of the API that is used to get the configuration.</param>
    /// <param name="workingDirectory">The directory that the parsed configuration is saved to.</param>
    /// <returns>Returns the BuilderConfiguration so that it can be used to add the SecretResolver to the DI container.</returns>
    public static BuilderConfiguration AddBuilderConfiguration(this IServiceCollection services, string application, string environment, string apiUri, string workingDirectory)
    {
        var configSettings = new BuilderConfiguration
        {
            Application = application,
            Environment = environment,
            RootApiUri = apiUri,
            WorkingDirectory = workingDirectory
        };
        services.AddSingleton(configSettings);
        return configSettings;
    }
}
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using pote.Config.Shared;
using pote.Config.Shared.Secrets;

namespace pote.Config.Middleware.Web;

public static class ExtensionMethods
{
    /// <summary>Adds the configuration from the API to the WebApplicationBuilder.</summary>
    /// <param name="builder">The WebApplicationBuilder to add the configuration to.</param>
    /// <param name="configuration">The BuilderConfiguration that is used to get the application, environment and API URI.</param>
    /// <param name="inputJson">The JSON that is sent to the API to get the configuration.</param>
    /// <param name="apiKey">The API key that is used to authenticate with the API.</param>
    /// <param name="errorOutput">The action that is called when an error occurs. The first parameter is the error message and the second parameter is the exception that was thrown.</param>
    /// <returns>Returns the WebApplicationBuilder with the configuration added.</returns>
    /// <exception cref="InvalidDataException">Thrown when the response from the API is empty.</exception>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static async Task<WebApplicationBuilder> AddConfigurationFromApi(this WebApplicationBuilder builder, BuilderConfiguration configuration, string inputJson, string apiKey, Action<string, Exception> errorOutput = null!)
    {
        var parsedJsonFile = Path.Combine(configuration.WorkingDirectory, $"appsettings.{configuration.Environment}.Parsed.json");
        try
        {
            var apiCommunication = new ApiCommunication(configuration.RootApiUri, CreateHttpClient(apiKey));
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
    
    // /// <summary>
    // /// Reads the options from appsettings, adds the SecretResolver to the options and adds the options to the DI container.
    // /// </summary>
    // /// <param name="services">The IServiceCollection to add the options to.</param>
    // /// <param name="configuration">The IConfiguration to read the options from.</param>
    // /// <param name="secretResolver">The SecretResolver that was created in AddSecretsResolver.</param>
    // /// <typeparam name="T">The options type that are being read from appsettings.</typeparam>
    // /// <returns>Returns the options so that they can be used in during startup.</returns>
    // public static T AddSecretConfiguration<T>(this IServiceCollection services, IConfiguration configuration, ISecretResolver secretResolver) where T : class, ISecretSettings
    // {
    //     var settings = configuration.GetSection(typeof (T).Name).Get<T>()!;
    //     settings.SecretResolver = secretResolver;
    //     services.AddSingleton<T>(_ => settings);
    //     return settings;
    // }
    
    /// <summary>Adds the BuilderConfiguration to the DI container.</summary>
    /// <param name="services">The IServiceCollection to add the BuilderConfiguration to.</param>
    /// <param name="application">The name of the application that is being configured.</param>
    /// <param name="environment">The environment that the application is running in.</param>
    /// <param name="rootApiUri">The URI of the API that is used to get the configuration.</param>
    /// <param name="workingDirectory">The directory that the parsed configuration is saved to.</param>
    /// <returns>Returns the BuilderConfiguration so that it can be used to add the SecretResolver to the DI container.</returns>
    public static BuilderConfiguration AddBuilderConfiguration(this IServiceCollection services, string application, string environment, string rootApiUri, string workingDirectory)
    {
        var configSettings = new BuilderConfiguration
        {
            Application = application,
            Environment = environment,
            RootApiUri = rootApiUri,
            WorkingDirectory = workingDirectory
        };
        services.AddSingleton(configSettings);
        return configSettings;
    }
    
    private static HttpClient CreateHttpClient(string apiKey)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        return client;
    }
    
    
    /// <summary>
    /// If the call to the API fails, this method is called to load the previously parsed configuration.
    /// If the previously parsed configuration is not found, an exception is thrown.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder to add the configuration to.</param>
    /// <param name="file">The file that contains the previously parsed configuration.</param>
    /// <returns>The WebApplicationBuilder with the configuration added.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the previously parsed configuration file is not found.</exception>
    private static WebApplicationBuilder AddPreviouslyParsedConfiguration(this WebApplicationBuilder builder, string file)
    {
        if (!File.Exists(file))
            throw new FileNotFoundException($"An old parsed configuration not found, file: {file}");
        builder.Configuration.AddJsonFile(file, false, true);
        return builder;
    }
}

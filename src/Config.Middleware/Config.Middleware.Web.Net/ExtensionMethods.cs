using System;
using System.IO;
using System.Linq;
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
            await SaveToPersistentCacheAsync(configuration, json, errorOutput);
            builder.Configuration.AddJsonFile(parsedJsonFile, false, false);
            return builder;
        }
        catch (Exception ex)
        {
            errorOutput("Error getting configuration from API. Loading previously parsed configuration.", ex);
            return builder.AddFallbackConfiguration(parsedJsonFile, configuration, errorOutput);
        }
    }

    private static WebApplicationBuilder AddFallbackConfiguration(this WebApplicationBuilder builder, string parsedJsonFile, BuilderConfiguration configuration, Action<string, Exception> errorOutput)
    {
        if (File.Exists(parsedJsonFile))
        {
            builder.Configuration.AddJsonFile(parsedJsonFile, false, true);
            return builder;
        }

        var persistentFile = GetPersistentCachePath(configuration);
        if (persistentFile != null && File.Exists(persistentFile))
        {
            errorOutput?.Invoke($"Local parsed configuration not found. Loading from persistent cache: {persistentFile}", null!);
            builder.Configuration.AddJsonFile(persistentFile, false, true);
            return builder;
        }

        throw new FileNotFoundException(
            $"No configuration found. Tried:{Environment.NewLine}" +
            $"  1. Config service API at {configuration.RootApiUri}{Environment.NewLine}" +
            $"  2. Local file: {parsedJsonFile}{Environment.NewLine}" +
            $"  3. Persistent cache: {persistentFile ?? "(disabled)"}");
    }

    private static async Task SaveToPersistentCacheAsync(BuilderConfiguration configuration, string json, Action<string, Exception> errorOutput)
    {
        try
        {
            var persistentFile = GetPersistentCachePath(configuration);
            if (persistentFile == null) return;

            var dir = Path.GetDirectoryName(persistentFile)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            await File.WriteAllTextAsync(persistentFile, json);
        }
        catch (Exception ex)
        {
            errorOutput?.Invoke("Failed to save persistent configuration cache. This is non-fatal.", ex);
        }
    }

    private static string? GetPersistentCachePath(BuilderConfiguration configuration)
    {
        if (!configuration.EnablePersistentCache) return null;

        var app = SanitizeDirectoryName(configuration.Application);
        var env = SanitizeDirectoryName(configuration.Environment);
        return Path.Combine(configuration.PersistentCacheBaseDirectory, app, env, $"appsettings.{env}.Parsed.json");
    }

    private static string SanitizeDirectoryName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Where(c => !invalid.Contains(c)).ToArray()).ToLowerInvariant();
    }

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
}

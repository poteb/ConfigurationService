using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using pote.Config.Shared;

namespace pote.Config.Middleware;

public static class ExtensionMethods
{
    public static async Task<IConfigurationBuilder> AddConfigurationFromApi(this IConfigurationBuilder builder, BuilderConfiguration configuration, string inputJson, Func<HttpClient> clientProvider, Action<string, Exception> errorOutput)
    {
        var parsedJsonFile = Path.Combine(configuration.WorkingDirectory, $"appsettings.{configuration.Environment}.Parsed.json");
        try
        {
            var apiCommunication = new ApiCommunication(configuration.RootApiUri, clientProvider.Invoke());
            var response = await apiCommunication.GetConfiguration(new ParseRequest(configuration.Application, configuration.Environment, inputJson));
            if (response == null)  throw new InvalidDataException("Reponse from API was empty.");
            var json = response.GetJson();
            await File.WriteAllTextAsync(parsedJsonFile, json);
            await SaveToPersistentCacheAsync(configuration, json, errorOutput);
            return builder.AddJsonFile(parsedJsonFile, false, false);
        }
        catch (Exception ex)
        {
            errorOutput?.Invoke("Error getting configuration from API. Loading previously parsed configuration.", ex);
            return builder.AddFallbackConfiguration(parsedJsonFile, configuration, errorOutput);
        }
    }

    private static IConfigurationBuilder AddFallbackConfiguration(this IConfigurationBuilder builder, string parsedJsonFile, BuilderConfiguration configuration, Action<string, Exception> errorOutput)
    {
        if (File.Exists(parsedJsonFile))
            return builder.AddJsonFile(parsedJsonFile, false, true);

        var persistentFile = GetPersistentCachePath(configuration);
        if (persistentFile != null && File.Exists(persistentFile))
        {
            errorOutput?.Invoke($"Local parsed configuration not found. Loading from persistent cache: {persistentFile}", null!);
            return builder.AddJsonFile(persistentFile, false, true);
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

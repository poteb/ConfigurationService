using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
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
            File.WriteAllText(parsedJsonFile, json);
            SaveToPersistentCache(configuration, json, errorOutput);
            return builder.AddJsonFile(parsedJsonFile, false, false);
        }
        catch (Exception ex)
        {
            errorOutput("Error getting configuration from API. Loading previously parsed configuration.", ex);
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
            $"No configuration found. Tried:{System.Environment.NewLine}" +
            $"  1. Config service API at {configuration.RootApiUri}{System.Environment.NewLine}" +
            $"  2. Local file: {parsedJsonFile}{System.Environment.NewLine}" +
            $"  3. Persistent cache: {persistentFile ?? "(disabled)"}");
    }

    private static void SaveToPersistentCache(BuilderConfiguration configuration, string json, Action<string, Exception> errorOutput)
    {
        try
        {
            var persistentFile = GetPersistentCachePath(configuration);
            if (persistentFile == null) return;

            var dir = Path.GetDirectoryName(persistentFile)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(persistentFile, json);
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
}

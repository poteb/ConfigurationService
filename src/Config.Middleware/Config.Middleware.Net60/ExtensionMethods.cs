using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using pote.Config.Shared;

namespace pote.Config.Middleware;

public static class ExtensionMethods
{
    public static async Task<WebApplicationBuilder> AddConfigurationFromApi(this WebApplicationBuilder builder, BuilderConfiguration configuration, string inputJson, Action<string, Exception> errorOutput = null!)
    {
        var parsedJsonFile = Path.Combine(configuration.WorkingDirectory, $"appsettings.{configuration.Environment}.Parsed.json");
        try
        {
            var apiCommunication = new ApiCommunication(configuration.ApiUri);
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
    
    public static async Task<IConfigurationBuilder> AddConfigurationFromApi(this IConfigurationBuilder builder, BuilderConfiguration configuration, string inputJson, Action<string, Exception> errorOutput = null!)
    {
        var parsedJsonFile = Path.Combine(configuration.WorkingDirectory, $"appsettings.{configuration.Environment}.Parsed.json");
        try
        {
            var apiCommunication = new ApiCommunication(configuration.ApiUri);
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
    
    private static WebApplicationBuilder AddPreviouslyParsedConfiguration(this WebApplicationBuilder builder, string file)
    {
        if (!File.Exists(file))
            throw new FileNotFoundException($"An old parsed configuration not found, file: {file}");
        builder.Configuration.AddJsonFile(file, false, true);
        return builder;
    }
    
    private static IConfigurationBuilder AddPreviouslyParsedConfiguration(this IConfigurationBuilder builder, string file)
    {
        if (!File.Exists(file))
            throw new FileNotFoundException($"An old parsed configuration not found, file: {file}");
        return builder.AddJsonFile(file, false, true);
    }
}
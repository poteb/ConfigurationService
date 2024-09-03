﻿using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using pote.Config.Shared;

namespace pote.Config.Middleware;

public interface ISecretResolver
{
    Task<string> ResolveSecret(string secret);
}

public class SecretResolver : ISecretResolver
{
    private const string RefPatternQuotes = "\\$refs:(?<ref>[^#]*)#?[^\"]*";
    private readonly BuilderConfiguration _configuration;
    private readonly Func<HttpClient> _clientProvider;

    public SecretResolver(BuilderConfiguration configuration, Func<HttpClient> clientProvider)
    {
        _configuration = configuration;
        _clientProvider = clientProvider;
    }

    public async Task<string> ResolveSecret(string secret)
    {
        var secretName = secret;
        var match = Regex.Match(secret, RefPatternQuotes);
        if (match.Success)
            secretName = match.Groups["ref"].Value;
        var client = _clientProvider();
        var request = new SecretValueRequest
        {
            SecretName = secretName,
            Application = "b0876c9d-c46f-4da1-9b92-5f8d80eddbdd",
            Environment = "5213b39e-9b17-4d91-bd4b-e54aacddbb49"
        };
        var response = await client.PostAsJsonAsync($"{_configuration.ApiUri}/Secrets/", request);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Error getting secret {secretName} from API. Status code: {response.StatusCode}");
        var responseBody = await response.Content.ReadFromJsonAsync<SecretValueResponse>();
        return responseBody!.Value;
    }
}
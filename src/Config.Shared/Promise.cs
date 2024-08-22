// namespace pote.Config.Shared;
//
// public class Promise
// {
//     
// }
//
// public interface ISecretResolver
// {
//     Task<T> ResolveSecret<T>(string secret);
// }
//
// public class SecretResolver : ISecretResolver
// {
//     private readonly string _apiUri;
//     private readonly Func<HttpClient> _clientProvider;
//
//     public SecretResolver(string apiUri, Func<HttpClient> clientProvider)
//     {
//         _apiUri = apiUri;
//         _clientProvider = clientProvider;
//     }
//
//     public async Task<T> ResolveSecret<T>(string secret)
//     {
//         var client = _clientProvider();
//         var response = await client.GetAsync($"{_apiUri}/secrets/{secret}");
//         if (!response.IsSuccessStatusCode)
//             throw new Exception($"Error getting secret {secret} from API. Status code: {response.StatusCode}");
//         var responseBody = await response.Content.ReadAsStringAsync();
//         var secretObj = JsonSerializer.Deserialize<T>(responseBody);
//         return secretObj;
//     }
// }
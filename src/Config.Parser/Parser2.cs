using System.Text.Json;
using pote.Config.DataProvider.Interfaces;
using pote.Config.Shared;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Json.Path;
using Newtonsoft.Json.Linq;
using pote.Config.Encryption;

namespace pote.Config.Parser;

public class Parser2 : IParser
{
    private readonly IDataProvider _dataProvider;

    private const string RefPatternQuotes = "\\$ref:(?<ref>[^#]*)#(?<field>[^\"]*)";
    private const string RefPatternParentheses = @"\(\$ref:(?<ref>[^#]*)#(?<field>[^\)]*)\)";
    
    public Action<string, string> TrackingAction { get; set; } = (_, _) => { };
    
    public Parser2(IDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }
    
    public async Task<string> Parse(string json, string application, string environment, Action<string> problems, CancellationToken cancellationToken, string encryptionKey, string rootId = "")
    {
        // Pre process
        if (string.IsNullOrWhiteSpace(json))
        {
            problems("Input json is empty.");
            return "";
        }

        var root = JsonNode.Parse(json)?.AsObject();
        if (root == null)
        {
            problems("Input json is empty.");
            return "";
        }
        var applications = await _dataProvider.GetApplications(cancellationToken);
        var dbApplication = applications.FirstOrDefault(s =>
            s.Id == application || s.Name.Equals(application, StringComparison.InvariantCultureIgnoreCase));
        if (dbApplication == null)
        {
            problems($"Application {application} not found");
            return "";
        }

        var environments = await _dataProvider.GetEnvironments(cancellationToken);
        var dbEnvironment = environments.FirstOrDefault(e =>
            e.Id == environment || e.Name.Equals(environment, StringComparison.InvariantCultureIgnoreCase));
        if (dbEnvironment == null)
        {
            problems($"Environment {environment} not found");
            return "";
        }

        // Process
        await RecurseJsonObject(root, new Variables(dbApplication.Id, dbEnvironment.Id, rootId, new List<KeyValuePair<string, string>>(), encryptionKey, problems, cancellationToken));
        

        // Post process
        MoveBaseChildrenToRoot(root);
        root.Add("generated_timestamp_utc", DateTime.UtcNow.ToString("s"));
        return root.ToString();
    }
    
    private async Task RecurseJsonObject(JsonObject obj, Variables variables)
    {
        foreach (var child in obj)
        {
            if (child.Value == null) continue;
            var type = child.Value.GetType();
        
            // first check if it's an object, and if so, recurse
            if (type == typeof(JsonObject))
            {
                var childObject = child.Value.AsObject();
                await RecurseJsonObject(childObject, variables);
                continue;
            }

            // then check if it's an array, and if so, recurse each element
            if (type == typeof(JsonArray))
            {
                foreach (var ca in child.Value.AsArray())
                {
                    if (ca == null) continue;
                    var caType = ca.GetType();
                    if (caType != typeof(JsonObject)) continue;
                    var caObject = ca.AsObject();
                    await RecurseJsonObject(caObject, variables);
                }

                continue;
            }
        
            // then it's a value
            if (child.Value.GetValueKind() != JsonValueKind.String) continue;
            if (type == typeof(JsonValue))
            {
                var value = child.Value.AsValue();
                var valueType = value.GetType();
                if (valueType != typeof(string)) continue;
                await HandleJsonValue(child.Value, variables);
                //Console.WriteLine($"{child.Key}: {child.Value} - {child.Value?.GetType()}");
            }
        }
    }

    private async Task HandleJsonValue(JsonNode jsonValue, Variables variables)
    {
        var value = jsonValue.ToString();
        var match = Regex.Match(value, RefPatternParentheses);
        var matchTypeParantheses = true;
        if (!match.Success)
        {
            match = Regex.Match(value, RefPatternQuotes);
            if (!match.Success)
                return;
            matchTypeParantheses = false;
        }
        
        // A match is found, now get the value from the database
        var configurationName = match.Groups[1].Value;
        if (variables.FetchedConfigurations.Any(pair => pair.Key.Equals(configurationName) && pair.Value.Equals(value)))
        {
            variables.Problems($"Referenced configuration {configurationName} was already resolved from source configuration {variables.SourceConfigurationId} and reference {value}.");
            return;
        }
        
        var configuration = await _dataProvider.GetConfiguration(configurationName, variables.Application, variables.Environment, variables.CancellationToken);
        variables.FetchedConfigurations.Add(new KeyValuePair<string, string>(configurationName, value));
        if (string.IsNullOrWhiteSpace(configuration.Json))
        {
            variables.Problems($"Reference could not be resolved, {match.Groups[0].Value}");
            return;
        }
        
        // Decrypt the value if needed
        if (configuration.IsJsonEncrypted)
            configuration.Json = EncryptionHandler.Decrypt(configuration.Json, variables.EncryptionKey);

        // Replace the value with the value from the database
        var refNode = JsonNode.Parse(configuration.Json);
        var refField = match.Groups["field"];
        var path = JsonPath.Parse($"$.{refField.Value}");
        var pathResult = path.Evaluate(refNode);
        if (!pathResult.Matches?.Any() ?? false)
        {
            variables.Problems($"Reference could not be resolved, {refField.Value}");
            return;
        }
        if (matchTypeParantheses)
            jProp.Value = new JValue(jProp.Value.ToString().Replace(match.Value, refToken.ToString()));
        else
            jProp.Value = refToken;
        
        //var refToken = refO..SelectToken(refField.Value);
        //if (refToken == null) return;
    }
    
    private void MoveBaseChildrenToRoot(JsonObject root)
    {
        var baseNode = root["base"];
        if (baseNode == null) return;
        foreach (var child in baseNode.AsObject())
        {
            root.Add(child.Key, child.Value);
        }
        root.Remove("base");
    }
    
    private class Variables
    {
        public Variables(string application, string environment, string sourceConfigurationId, List<KeyValuePair<string, string>> fetchedConfigurations, string encryptionKey, Action<string> problems, CancellationToken cancellationToken)
        {
            Application = application;
            Environment = environment;
            SourceConfigurationId = sourceConfigurationId;
            FetchedConfigurations = fetchedConfigurations;
            EncryptionKey = encryptionKey;
            Problems = problems;
            CancellationToken = cancellationToken;
        }

        public string Application { get; set; }
        public string Environment { get; set; }
        public string SourceConfigurationId { get; set; }
        public List<KeyValuePair<string, string>> FetchedConfigurations { get; set; }
        public string EncryptionKey { get; set; }
        public Action<string> Problems { get; set; }
        public CancellationToken CancellationToken { get; set; }
    }
}
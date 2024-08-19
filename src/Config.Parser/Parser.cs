using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using pote.Config.DataProvider.Interfaces;
using pote.Config.Encryption;
using pote.Config.Shared;

namespace pote.Config.Parser;

public class Parser : IParser
{
    private readonly IDataProvider _dataProvider;

    private const string RefPatternQuotes = "\\$ref:(?<ref>[^#]*)#(?<field>[^\"]*)";
    private const string RefPatternParentheses = "\\(\\$ref:(?<ref>[^#]*)#(?<field>[^\\)]*)\\)";
    //private const string NamePattern = "(?<name>[^.]*).?(?<application>[^.]*).?(?<environment>[^.]*)";

    /// <summary>Add your own value to gather tracking data</summary>
    public Action<string, string> TrackingAction { get; set; } = (_, _) => { };

    public Parser(IDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    /// <summary>Parses the specified configuration. Will do some pre- and post-processing.</summary>
    public async Task<string> Parse(string json, string application, string environment, Action<string> problems,
        CancellationToken cancellationToken, string encryptionKey, string rootId = "")
    {
        // Pre process
        if (string.IsNullOrWhiteSpace(json))
        {
            problems("Input json is empty.");
            return "";
        }

        var root = JObject.Parse(json);
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
        await HandleToken(root, dbApplication.Id, dbEnvironment.Id, problems, cancellationToken, rootId, new List<KeyValuePair<string, string>>(), encryptionKey);

        // Post process
        MoveBaseChildrenToRoot(root);
        root.AddFirst(new JProperty("generated_timestamp_utc", DateTime.UtcNow.ToString("s")));
        return root.ToString();
    }

    /// <summary>Method for recursively handling the tokens in the json</summary>
    /// <param name="sourceConfigurationId">Used to track what's happening.</param>
    [SuppressMessage("ReSharper", "InvalidXmlDocComment")]
    private async Task HandleToken(JToken token, string application, string environment, Action<string> problems, CancellationToken cancellationToken, string sourceConfigurationId, List<KeyValuePair<string, string>> fetchedConfigurations, string encryptionKey)
    {
        while (true)
        {
            // Recurse into children
            if (token.HasValues)
                foreach (var child in token.Children())
                    await HandleToken(child, application, environment, problems, cancellationToken, sourceConfigurationId, fetchedConfigurations, encryptionKey);

            // Check token for different types
            if (token.Type != JTokenType.Property) return;
            var jProp = token.Value<JProperty>();
            if (jProp == null || jProp.Value.Type != JTokenType.String) return;
            var value = jProp.Value.Value<string>();
            if (value == null) return;

            // Check if the value is a reference
            var match = Regex.Match(value, RefPatternParentheses);
            var matchTypeParantheses = true;
            if (!match.Success)
            {
                match = Regex.Match(value, RefPatternQuotes);
                if (!match.Success)
                    return;
                matchTypeParantheses = false;
            }

            // A match is found, now get the value from the database.
            var configurationName = match.Groups[1].Value;
            if (fetchedConfigurations.Any(pair => pair.Key.Equals(configurationName) && pair.Value.Equals(value)))
            {
                problems($"Referenced configuration {configurationName} was already resolved from source configuration {sourceConfigurationId} and reference {value}.");
                return;
            }

            var configuration = await _dataProvider.GetConfiguration(configurationName, application, environment, cancellationToken);
            fetchedConfigurations.Add(new KeyValuePair<string, string>(configurationName, value));
            if (string.IsNullOrWhiteSpace(configuration.Json))
            {
                problems($"Reference could not be resolved, {match.Groups[0].Value}");
                return;
            }

            if (configuration.IsSecret)
            {
                // Don't parse secrets. Convert it to a promise.
                jProp.Value = value.Replace("$ref:","$refp:");
                return;
            }

            // Decrypt the value if needed
            if (configuration.IsJsonEncrypted)
                configuration.Json = EncryptionHandler.Decrypt(configuration.Json, encryptionKey);

            var refO = JObject.Parse(configuration.Json);
            var refField = match.Groups["field"];
            var refToken = refO.SelectToken(refField.Value);
            if (refToken == null) return;

            // Replace the value with the value from the database.
            //jProp.Value = refToken;
            //jProp.Value.Replace(match.Value, refToken.ToString());
            if (matchTypeParantheses)
                jProp.Value = new JValue(jProp.Value.ToString().Replace(match.Value, refToken.ToString()));
            else
                jProp.Value = refToken;

            TrackingAction(sourceConfigurationId, configuration.HeaderId);
            sourceConfigurationId = configuration.HeaderId;

            if (string.IsNullOrEmpty(refField.Value) && refToken.HasValues)
            {
                // The field has no value, so it might have some children that we can work with. Overwrite token to the current JObject and continue. This is like a recursive call.
                token = refO;
            }
        }
    }

    /// <summary>If a child of the root object is named 'Base' or 'base', it's child object are moved to the root. This comes into play when the input JSON needs to be completely replaced by the content of a configuration (not just the value but also the name). In case of name clash of elements in root, the existing element will be overwritten; Data from the configuration service wins.</summary>
    /// <example>
    /// Before:
    /// {
    ///    "Base": {
    ///        "Spacecraft": {
    ///            "Name": "Far Far Away"
    ///        }
    ///    }
    /// }
    /// After:
    /// {
    ///    "Spacecraft": {
    ///        "Name": "Far Far Away"
    ///    }
    /// }
    /// </example>
    private static void MoveBaseChildrenToRoot(JObject root)
    {
        var baseObj = root.Property("Base") ?? root.Property("base");
        if (baseObj == null) return;
        foreach (var child in baseObj.Value.Children<JProperty>())
        {
            var existing = root.Property(child.Name);
            existing?.Remove();
            root[child.Name] = child.Value;
        }

        baseObj.Remove();
    }
}
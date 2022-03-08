using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using pote.Config.Shared;

namespace pote.Config.Parser
{
    public class Parser : IParser
    {
        private readonly IDataProvider _dataProvider;
        private const string RefPattern = "\\$ref:(?<ref>[^#]*)#(?<field>[^\"]*)";
        //private const string NamePattern = "(?<name>[^.]*).?(?<application>[^.]*).?(?<environment>[^.]*)";

        public Parser(IDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        public async Task<string> Parse(string json, string application, string environment, Action<string> problems, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                problems("Input json is empty.");
                return "";
            }
            var root = JObject.Parse(json);
            var applications = await _dataProvider.GetApplications(cancellationToken);
            var dbApplications = applications.FirstOrDefault(s => s.Id == application || s.Name.Equals(application, StringComparison.InvariantCultureIgnoreCase));
            if (dbApplications == null)
            {
                problems($"Application {application} not found");
                return "";
            }

            var environments = await _dataProvider.GetEnvironments(cancellationToken);
            var dbEnv = environments.FirstOrDefault(e => e.Id == environment || e.Name.Equals(environment, StringComparison.InvariantCultureIgnoreCase));
            if (dbEnv == null)
            {
                problems($"Environment {environment} not found");
                return "";
            }
            
            await HandleToken(root, dbApplications.Id, dbEnv.Id, problems, cancellationToken);
            MoveBaseChildrenToRoot(root);
            root.AddFirst(new JProperty("generated_timestamp_utc", DateTime.UtcNow.ToString("s")));
            return root.ToString();
        }

        private async Task HandleToken(JToken token, string application, string environment, Action<string> problems, CancellationToken cancellationToken)
        {
            if (token.HasValues)
                foreach (var child in token.Children())
                    await HandleToken(child, application, environment, problems, cancellationToken);
            if (token.Type != JTokenType.Property) return;
            var jProp = token.Value<JProperty>();
            if (jProp == null || jProp.Value.Type != JTokenType.String) return;
            var value = jProp.Value.Value<string>();
            if (value == null) return;
            var match = Regex.Match(value, RefPattern);
            if (!match.Success) return;
            var json = await _dataProvider.GetConfigurationJson(match.Groups[1].Value, application, environment, cancellationToken);
            if (string.IsNullOrEmpty(json))
            {
                problems($"Reference could not be resolved, {match.Groups[0].Value}");
                return;
            }
            var refO = JObject.Parse(json);
            var refField = match.Groups["field"];
            var refToken = refO.SelectToken(refField.Value);
            if (refToken == null) return;
            jProp.Value = refToken;
            if (string.IsNullOrEmpty(refField.Value))
                await HandleToken(refO, application, environment, problems, cancellationToken);
        }

        /// <summary>If a child of the root object is named 'Base' or 'base', it's child object are moved to the root. This comes into play when the input JSON needs to be completely replaced by the content of a configuration (not just the value but also the name).</summary>
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
            root.Add(baseObj.Value.Children<JProperty>());
            baseObj.Remove();
        }
    }
}
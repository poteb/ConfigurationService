using System.Text.RegularExpressions;
using pote.Config.Admin.Api.Mappers;
using pote.Config.Admin.Api.Model;
using pote.Config.Admin.Api.Model.DependencyGraph;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.DataProvider.Interfaces;

namespace pote.Config.Admin.Api.Services;

public interface IDependencyGraphService
{
    Task<DependencyGraphResponse> GetDependencyGraphAsync(CancellationToken cancellationToken);
}

public class DependencyGraphService : IDependencyGraphService
{
    private readonly IAdminDataProvider _adminDataProvider;
    public const string CacheName = "dependencyGraph";
    private const string RefPattern = "\\$ref:(?<ref>[^#]*)#";

    public DependencyGraphService(IAdminDataProvider adminDataProvider)
    {
        _adminDataProvider = adminDataProvider;
    }

    public async Task<DependencyGraphResponse> GetDependencyGraphAsync(CancellationToken cancellationToken)
    {
        var parser = new Parser.Parser(_adminDataProvider);
        var graph = new DependencyGraphResponse();

        var environments = await _adminDataProvider.GetEnvironments(cancellationToken);
        var applications = await _adminDataProvider.GetApplications(cancellationToken);
        var configurationHeadersDb = await _adminDataProvider.GetAll(cancellationToken);
        var configurationHeadersApi = ConfigurationMapper.ToApi(configurationHeadersDb, applications, environments);

        parser.TrackingAction = (from, to) =>
        {
            var fromVertex = graph.Vertices.FirstOrDefault(v => v.Id == from);
            var toVertex = graph.Vertices.FirstOrDefault(v => v.Id == to);
            if (fromVertex != null && toVertex != null)
            {
                graph.Edges.Add(new Edge(fromVertex.Id, toVertex.Id, fromVertex.Name, toVertex.Name));
            }
        };

        var referenceMap = GetReferenceMap(configurationHeadersApi);

        foreach (var application in applications)
        {
            graph.Vertices.Add(new Vertex(application.ToApi().Id, application.Name, typeof(Application).ToString()));
        }

        foreach (var environment in environments)
        {
            graph.Vertices.Add(new Vertex(environment.ToApi().Id, environment.Name, typeof(Model.Environment).ToString()));
        }

        foreach (var header in configurationHeadersDb)
        {
            var vertex = new Vertex(header.Id, header.Name, typeof(Configuration).ToString());
            graph.Vertices.Add(vertex);

            var headerApplications = header.Configurations.SelectMany(c => c.Applications).Distinct();
            var headerEnvironments = header.Configurations.SelectMany(c => c.Environments).Distinct();

            foreach (var headerApplication in headerApplications)
            {
                var toVertex = graph.Vertices.FirstOrDefault(v => v.Id == headerApplication);
                if (toVertex == null) continue;
                var edge = new Edge(vertex.Id, toVertex.Id, vertex.Name, toVertex.Name);
                vertex.Edges.Add(edge.Id);
                toVertex.Edges.Add(edge.Id);
                graph.Edges.Add(edge);
            }
            
            foreach (var headerEnvironment in headerEnvironments)
            {
                var toVertex = graph.Vertices.FirstOrDefault(v => v.Id == headerEnvironment);
                if (toVertex == null) continue;
                var edge = new Edge(vertex.Id, toVertex.Id, vertex.Name, toVertex.Name);
                vertex.Edges.Add(edge.Id);
                toVertex.Edges.Add(edge.Id);
                graph.Edges.Add(edge);
            }
        }

        // Has to loop again so that all vertices are added to the graph
        foreach (var header in configurationHeadersDb)
        {
            var vertex = graph.Vertices.First(v => v.Id == header.Id);
            
            foreach (var pair in referenceMap.Where(r => r.Key.Id == header.Id))
            {
                var toVertex = graph.Vertices.First(v => v.Id == pair.Value.Id);
                var edge = new Edge(vertex.Id, toVertex.Id, vertex.Name, toVertex.Name);
                vertex.Edges.Add(edge.Id);
                toVertex.Edges.Add(edge.Id);
                graph.Edges.Add(edge);
            }
        }

        return graph;
    }

    /// <summary>Creates a list of key value pairs of configuration header and the configuration header it references.</summary>
    public List<KeyValuePair<ConfigurationHeader, ConfigurationHeader>> GetReferenceMap(List<ConfigurationHeader> headers)
    {
        var pairs = new List<KeyValuePair<ConfigurationHeader, ConfigurationHeader>>();
        foreach (var header in headers)
        {
            foreach (var configuration in header.Configurations)
            {
                var matches = Regex.Matches(configuration.Json, RefPattern).Cast<Match>();
                foreach (var match in matches)
                {
                    var refName = match.Groups[1].Value;
                    var targetHeader = headers.FirstOrDefault(h => h.Name == refName);
                    if (targetHeader == null) continue;
                    if (pairs.Any(p => p.Key.Id == header.Id && p.Value.Id == targetHeader.Id)) continue;
                    pairs.Add(new KeyValuePair<ConfigurationHeader, ConfigurationHeader>(header, targetHeader));
                }
            }
        }

        return pairs;
    }
}
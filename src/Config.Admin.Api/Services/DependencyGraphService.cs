using pote.Config.Admin.Api.Mappers;
using pote.Config.Admin.Api.Model;
using pote.Config.Admin.Api.Model.DependencyGraph;
using pote.Config.Admin.Api.Model.RequestResponse;
using pote.Config.DataProvider.Interfaces;
using pote.Config.Shared;

namespace pote.Config.Admin.Api.Services;

public interface IDependencyGraphService
{
    Task<DependencyGraphResponse> GetDependencyGraphAsync(CancellationToken cancellationToken);
}

public class DependencyGraphService : IDependencyGraphService
{
    private readonly IAdminDataProvider _adminDataProvider;
    private readonly IDataProvider _dataProvider;
    public const string CacheName = "dependencyGraph";

    public DependencyGraphService(IAdminDataProvider adminDataProvider, IDataProvider dataProvider)
    {
        _adminDataProvider = adminDataProvider;
        _dataProvider = dataProvider;
    }

    public async Task<DependencyGraphResponse> GetDependencyGraphAsync(CancellationToken cancellationToken)
    {
        var parser = new Parser.Parser(_dataProvider);
        var graph = new DependencyGraphResponse();

        var environments = await _adminDataProvider.GetEnvironments(cancellationToken);
        var applications = await _adminDataProvider.GetApplications(cancellationToken);
        var configurationHeaders = await _adminDataProvider.GetAll(cancellationToken);

        parser.TrackingAction = (from, to) =>
        {
            var fromVertice = graph.Vertices.FirstOrDefault(v => v.Id == from);
            var toVertice = graph.Vertices.FirstOrDefault(v => v.Id == to);
            if (fromVertice != null && toVertice != null)
            {
                graph.Edges.Add(new Edge(fromVertice.Id, toVertice.Id));
            }
        };

        foreach (var application in applications)
        {
            graph.Vertices.Add(new Vertex<Application>(application.ToApi(), application.Name));
        }

        foreach (var environment in environments)
        {
            graph.Vertices.Add(new Vertex<Model.Environment>(environment.ToApi(), environment.Name));
        }

        foreach (var header in configurationHeaders)
        {
            foreach (var configuration in header.Configurations)
            {
                var apiConfiguration = configuration.ToApi(applications, environments);
                var vertex = new Vertex<Configuration>(apiConfiguration, header.Name);
                graph.Vertices.Add(vertex);
                
                // Add applications to edges
                foreach (var app in configuration.Applications)
                {
                    var toVertex = graph.Vertices.First(v => v.Id == app);
                    var edge = new Edge(vertex.Id, toVertex.Id);
                    vertex.Edges.Add(edge.Id);
                    toVertex.Edges.Add(edge.Id);
                    graph.Edges.Add(edge);
                }

                // Add environments to edges
                foreach (var env in configuration.Environments)
                {
                    var toVertex = graph.Vertices.First(v => v.Id == env);
                    var edge = new Edge(vertex.Id, toVertex.Id);
                    vertex.Edges.Add(edge.Id);
                    toVertex.Edges.Add(edge.Id);
                    graph.Edges.Add(new Edge(vertex.Id, toVertex.Id));
                }

                // foreach (var app in configuration.Applications)
                // {
                //     foreach (var env in configuration.Environments)
                //     {
                //         var _ = parser.Parse(configuration.Json, app, env, p => { }, cancellationToken, configuration.Id);
                //     }
                // }
            }
        }

        return graph;
    }
}
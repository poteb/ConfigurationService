using pote.Config.Admin.Api.Mappers;
using pote.Config.Admin.Api.Model;
using pote.Config.Admin.Api.Model.DependencyGraph;
using pote.Config.Admin.Api.Model.RequestResponse;
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
            if (graph.Vertices.ContainsKey(from) && graph.Vertices.ContainsKey(to))
            {
                graph.Edges.Add(graph.Vertices[from].Id, new Edge(graph.Vertices[from], graph.Vertices[to]));
            }
        };

        foreach (var application in applications)
        {
            graph.Vertices.Add(application.Id, new Vertex<Application>(application.ToApi(), () => application.Name));
        }
        
        foreach (var environment in environments)
        {
            graph.Vertices.Add(environment.Id, new Vertex<Model.Environment>(environment.ToApi(), () => environment.Name));
        }
        
        foreach (var header in configurationHeaders)
        {
            foreach (var configuration in header.Configurations)
            {
                var apiConfiguration = configuration.ToApi(applications, environments);
                var vertext = new Vertex<Configuration>(apiConfiguration, () => $"{header.Name} ({configuration.Id})");
                graph.Vertices.Add(configuration.Id, vertext);
                foreach (var app in configuration.Applications)
                {
                    graph.Edges.Add(vertext.Value.Id, new Edge(vertext, graph.Vertices[app]));
                }

                foreach (var env in configuration.Environments)
                {
                    graph.Edges.Add(vertext.Value.Id, new Edge(vertext, graph.Vertices[env]));
                }

                foreach (var app in configuration.Applications)
                {
                    foreach (var env in configuration.Environments)
                    {
                        var _ = parser.Parse(configuration.Json, app, env, p => { }, cancellationToken, configuration.Id);
                    }
                }
            }
        }

        return graph;
    }
}
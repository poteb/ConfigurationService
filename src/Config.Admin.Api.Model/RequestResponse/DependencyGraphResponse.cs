using pote.Config.Admin.Api.Model.DependencyGraph;

namespace pote.Config.Admin.Api.Model.RequestResponse;

public class DependencyGraphResponse
{
    public List<IVertex> Vertices { get; set; } = new();

    public List<Edge> Edges { get; set; } = new();
    // public List<ApplicationVertex> Applications { get; set; }
    // public List<EnvironmentVertex> Environments { get; set; }
    // public List<ConfigurationVertex> Configurations { get; set; }
}
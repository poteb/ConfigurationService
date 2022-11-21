using pote.Config.Admin.Api.Model.DependencyGraph;

namespace pote.Config.Admin.Api.Model.RequestResponse;

public class DependencyGraphResponse
{
    public Dictionary<string, IVertex> Vertices { get; set; }

    public Dictionary<string, Edge> Edges { get; set; }
    // public List<ApplicationVertex> Applications { get; set; }
    // public List<EnvironmentVertex> Environments { get; set; }
    // public List<ConfigurationVertex> Configurations { get; set; }
}
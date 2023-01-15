using pote.Config.Admin.Api.Model.DependencyGraph;

namespace pote.Config.Admin.Api.Model.RequestResponse;

public class DependencyGraphResponse
{
    public List<Vertex> Vertices { get; set; } = new();
    public List<Edge> Edges { get; set; } = new();
}
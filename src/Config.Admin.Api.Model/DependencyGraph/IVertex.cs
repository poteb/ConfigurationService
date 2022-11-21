namespace pote.Config.Admin.Api.Model.DependencyGraph;

public interface IVertex
{
    List<Edge> Edges { get; }
    string Name { get; }
    string Id { get; }
    
    public Type Type { get; }
}
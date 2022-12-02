namespace pote.Config.Admin.Api.Model.DependencyGraph;

public class Vertex<T> : IVertex where T : IIdentity
{
    public T Value { get; }

    public string Id => Value.Id;

    public List<string> Edges { get; } = new();
    public string Name { get; }
    
    public Vertex(T value, string name)
    {
        Name = name;
        Value = value;
    }
}
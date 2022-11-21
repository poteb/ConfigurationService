namespace pote.Config.Admin.Api.Model.DependencyGraph;

public class Vertex<T> : IVertex where T : IIdentity
{
    private readonly Func<string> _nameFunc;

    public T Value { get; }

    public string Id => Value.Id;

    public Type Type => typeof(T);
    public List<Edge> Edges { get; } = new();
    public string Name => _nameFunc();
    

    public Vertex(T value, Func<string> nameFunc)
    {
        _nameFunc = nameFunc;
        Value = value;
    }

}
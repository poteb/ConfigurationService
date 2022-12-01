namespace pote.Config.Admin.Api.Model.DependencyGraph;

public class Edge
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public string From { get; }
    public string To { get; }
    
    public Edge(string from, string to)
    {
        From = from;
        To = to;
    }
}
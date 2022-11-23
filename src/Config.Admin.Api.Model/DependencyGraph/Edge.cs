namespace pote.Config.Admin.Api.Model.DependencyGraph;

public class Edge
{
    public IVertex From { get; }
    public IVertex To { get; }
    
    public Edge(IVertex from, IVertex to)
    {
        From = from;
        To = to;
    }
}
namespace pote.Config.Admin.Api.Model.DependencyGraph;

public record Edge(string FromId, string ToId, string FromName, string ToName)
{
    public string Id { get; } = Guid.NewGuid().ToString();
}
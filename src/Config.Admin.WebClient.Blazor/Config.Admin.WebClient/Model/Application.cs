namespace pote.Config.Admin.WebClient.Model;

public class Application : IEquatable<Application>
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public bool IsSelected { get; set; }

    public List<Config.Admin.Api.Model.DependencyGraph.Edge> Usages { get; } = new();

    public bool Equals(Application? other)
    {
        if (ReferenceEquals(other, null)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id) && Name.Equals(other.Name);
    }

    public override string ToString()
    {
        return Name;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode() ^ Name.GetHashCode();
    }
}
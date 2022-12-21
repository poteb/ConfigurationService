namespace pote.Config.Admin.WebClient.Model;

public class ConfigEnvironment : IEquatable<ConfigEnvironment>
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
    public bool IsSelected { get; set; }
    
    public bool Equals(ConfigEnvironment? other)
    {
        if (object.ReferenceEquals(other, null)) return false;
        if (object.ReferenceEquals(this, other)) return true;
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
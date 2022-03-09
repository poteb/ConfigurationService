namespace pote.Config.Admin.WebClient.Model;

public class Configuration : IEquatable<Configuration>
{
    public string HeaderId { get; set; } = string.Empty;
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Json { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public bool Deleted { get; set; }

    public List<ConfigEnvironment> Environments { get; set; } = new();
    public List<Application> Applications { get; set; } = new();
    public List<Configuration> History { get; set; } = new();
    public int Index { get; set; }


    public bool Equals(Configuration? other)
    {
        if (other == null) return false;
        if (!Id.Equals(other.Id)) return false;
        if (!Json.Equals(other.Json)) return false;
        if (!CreatedUtc.Equals(other.CreatedUtc)) return false;
        if (!IsActive == other.IsActive) return false;
        if (!Deleted == other.Deleted) return false;

        if (!Applications.Count.Equals(other.Applications.Count)) return false;
        if (!string.Join(",", Applications.Select(s => s.Id)).Equals(string.Join(",", other.Applications.Select(s => s.Id)))) return false;

        if (!Environments.Count.Equals(other.Environments.Count)) return false;
        if (!string.Join(",", Environments.Select(e => e.Id)).Equals(string.Join(",", other.Environments.Select(e => e.Id)))) return false;

        return true;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null) return false;
        if (obj is not Configuration other) return false;
        return Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, HeaderId, CreatedUtc, Json, Deleted, IsActive, Environments, Applications);
    }
}
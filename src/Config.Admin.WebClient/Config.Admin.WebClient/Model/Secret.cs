namespace pote.Config.Admin.WebClient.Model;

public class Secret : IEquatable<Secret>, IComparable<Secret>
{
    public string HeaderId { get; set; } = string.Empty;
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Value { get; set; } = string.Empty;
    public string ValueType { get; set; } = nameof(String);
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public bool Deleted { get; set; }
    public bool IsEncrypted { get; } = true;

    public List<ConfigEnvironment> Environments { get; set; } = new();
    public List<Application> Applications { get; set; } = new();
    public List<Secret> History { get; set; } = new();
    public int Index { get; set; }
    public bool IsNew { get; set; }

    public string EnvironmentsAsText => string.Join(", ", Environments.OrderBy(x => x.Name));
    public string ApplicationsAsText => string.Join(", ", Applications.OrderBy(x => x.Name));

    public bool Equals(Secret? other)
    {
        if (other == null) return false;
        if (!Id.Equals(other.Id)) return false;
        if (!Value.Equals(other.Value)) return false;
        if (!ValueType.Equals(other.ValueType)) return false;
        if (!CreatedUtc.Equals(other.CreatedUtc)) return false;
        if (!IsActive == other.IsActive) return false;
        if (!Deleted == other.Deleted) return false;
        if (!IsEncrypted == other.IsEncrypted) return false;

        if (!Applications.Count.Equals(other.Applications.Count)) return false;
        foreach (var application in Applications)
            if (other.Applications.All(o => o.Id != application.Id))
                return false;

        if (!Environments.Count.Equals(other.Environments.Count)) return false;
        foreach (var environment in Environments)
            if (other.Environments.All(e => e.Id != environment.Id))
                return false;

        return true;
    }

    public int CompareTo(Secret? other)
    {
        if (Index <= other?.Index) return -1;
        return 1;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null) return false;
        if (obj is not Configuration other) return false;
        return Equals(other);
    }

    public override int GetHashCode()
    {
        int applicationsHashCode = Applications.Aggregate(0, (current, application) => HashCode.Combine(current, application.GetHashCode()));
        int environmentsHashCode = Environments.Aggregate(0, (current, environment) => HashCode.Combine(current, environment.GetHashCode()));
        return HashCode.Combine(Id, HeaderId, CreatedUtc, Value, Deleted, IsActive, environmentsHashCode, applicationsHashCode);
    }
}
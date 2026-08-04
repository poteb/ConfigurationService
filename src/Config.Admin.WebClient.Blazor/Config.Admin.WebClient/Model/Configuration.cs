namespace pote.Config.Admin.WebClient.Model;

public class Configuration : IEquatable<Configuration>, IComparable<Configuration>
{
    public string HeaderId { get; set; } = string.Empty;
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Json { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public bool Deleted { get; set; }
    public bool IsJsonEncrypted { get; set; }

    public List<ConfigEnvironment> Environments { get; set; } = new();
    public List<Application> Applications { get; set; } = new();
    public List<Configuration> History { get; set; } = new();
    public int Index { get; set; }
    public bool IsNew { get; set; }

    public string EnvironmentsAsText => string.Join(", ", Environments.OrderBy(x => x.Name));
    public string ApplicationsAsText => string.Join(", ", Applications.OrderBy(x => x.Name));

    internal bool IsJsonEncryptedForced { get; set; }


    public bool Equals(Configuration? other)
    {
        int i = 0;
        if (other == null) return false;
        if (!Id.Equals(other.Id)) return false;
        if (!Json.Equals(other.Json)) return false;
        if (!CreatedUtc.Equals(other.CreatedUtc)) return false;
        if (!IsActive == other.IsActive) return false;
        if (!Deleted == other.Deleted) return false;
        Console.WriteLine(i++);
        Console.WriteLine($"{Id} == {other.Id}");
        if (!IsJsonEncrypted == other.IsJsonEncrypted) return false;

        if (!Applications.Count.Equals(other.Applications.Count)) return false;
        foreach (var application in Applications)
            if (other.Applications.All(o => o.Id != application.Id))
                return false;
        Console.WriteLine(i++);

        if (!Environments.Count.Equals(other.Environments.Count)) return false;
        foreach (var environment in Environments)
            if (other.Environments.All(e => e.Id != environment.Id))
                return false;
        Console.WriteLine(i++);

        return true;
    }

    public int CompareTo(Configuration? other)
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
        return HashCode.Combine(Id, HeaderId, CreatedUtc, Json, Deleted, IsActive, Environments, Applications);
    }
}
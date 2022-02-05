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
    public List<ConfigSystem> Systems { get; set; } = new();
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

        if (!Systems.Count.Equals(other.Systems.Count)) return false;
        if (!string.Join(",", Systems.Select(s => s.Id)).Equals(string.Join(",", other.Systems.Select(s => s.Id)))) return false;

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
        return HashCode.Combine(Id, HeaderId, CreatedUtc, Json, Deleted, IsActive, Environments, Systems);
    }
}

public class ConfigurationHeader : IEquatable<ConfigurationHeader>
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdateUtc { get; set; } = DateTime.UtcNow;
    public bool Deleted { get; set; }
    public bool IsActive { get; set; }
    public List<Configuration> Configurations { get; set; } = new();

    public bool Equals(ConfigurationHeader? other)
    {
        if (other == null) return false;
        if (!Id.Equals(other.Id)) return false;
        if (!Name.Equals(other.Name)) return false;
        if (!CreatedUtc.Equals(other.CreatedUtc)) return false;
        if (!UpdateUtc.Equals(other.UpdateUtc)) return false;
        if (!Deleted.Equals(other.Deleted)) return false;
        if (!IsActive.Equals(other.IsActive)) return false;
        if (Configurations.Count != other.Configurations.Count) return false;
        for (int i = 0; i < Configurations.Count; i++)
        {
            var me = Configurations[i];
            if (!me.Equals(other.Configurations[i])) return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null) return false;
        if (obj is not ConfigurationHeader other) return false;
        return Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name, CreatedUtc, UpdateUtc, Deleted, IsActive, Configurations);
    }
}
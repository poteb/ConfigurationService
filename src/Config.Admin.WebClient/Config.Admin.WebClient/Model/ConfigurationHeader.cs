namespace pote.Config.Admin.WebClient.Model;

public class ConfigurationHeader : IEquatable<ConfigurationHeader>
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdateUtc { get; set; } = DateTime.UtcNow;
    public bool Deleted { get; set; }
    public bool IsActive { get; set; } = true;
    public List<Configuration> Configurations { get; set; } = new();
    public bool IsJsonEncrypted { get; set; }
    
    internal bool IsJsonEncryptedForced { get; set; }

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
        for (var i = 0; i < Configurations.Count; i++)
        {
            var me = Configurations[i];
            if (!me.Equals(other.Configurations[i])) return false;
        }
        if (!IsJsonEncrypted.Equals(other.IsJsonEncrypted)) return false;

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
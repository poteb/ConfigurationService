namespace pote.Config.Admin.WebClient.Model;

public class SecretHeader : IEquatable<SecretHeader>
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdateUtc { get; set; } = DateTime.UtcNow;
    public bool Deleted { get; set; }
    public bool IsActive { get; set; } = true;
    public List<Secret> Secrets { get; set; } = new();
    public bool IsEncrypted { get; } = true;
    

    public bool Equals(SecretHeader? other)
    {
        if (other == null) return false;
        if (!Id.Equals(other.Id)) return false;
        if (!Name.Equals(other.Name)) return false;
        if (!CreatedUtc.Equals(other.CreatedUtc)) return false;
        if (!UpdateUtc.Equals(other.UpdateUtc)) return false;
        if (!Deleted.Equals(other.Deleted)) return false;
        if (!IsActive.Equals(other.IsActive)) return false;
        if (Secrets.Count != other.Secrets.Count) return false;
        for (var i = 0; i < Secrets.Count; i++)
        {
            var me = Secrets[i];
            if (!me.Equals(other.Secrets[i])) return false;
        }
        if (!IsEncrypted.Equals(other.IsEncrypted)) return false;

        return true;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null) return false;
        if (obj is not SecretHeader other) return false;
        return Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name, CreatedUtc, UpdateUtc, Deleted, IsActive, Secrets);
    }
}
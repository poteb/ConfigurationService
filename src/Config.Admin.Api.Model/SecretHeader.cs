namespace pote.Config.Admin.Api.Model;

public class SecretHeader : IIdentity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdateUtc { get; set; } = DateTime.UtcNow;
    public bool Deleted { get; set; }
    public bool IsActive { get; set; }
    public List<Secret> Secrets { get; set; } = new();
    public bool IsEncrypted { get; } = true;
}
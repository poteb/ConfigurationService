namespace pote.Config.Admin.Api.Model;

public class Secret : IIdentity
{
    public string HeaderId { get; set; } = string.Empty;
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Value { get; set; } = string.Empty;
    public string ValueType { get; set; } = nameof(String);
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public string Applications { get; set; } = string.Empty;
    public string Environments { get; set; } = string.Empty;
    public bool Deleted { get; set; }
    public bool IsEncrypted { get; set; } = true;
}
namespace pote.Config.DbModel;

public class Secret
{
    public string HeaderId { get; set; } = string.Empty;
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Value { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public List<string> Applications { get; set; } = new();
    public List<string> Environments { get; set; } = new();
    public bool Deleted { get; set; }
    public bool IsEncrypted { get; } = true;
}
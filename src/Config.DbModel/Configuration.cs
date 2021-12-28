namespace pote.Config.DbModel;

public class Configuration
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Gid { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Json { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public string Systems { get; set; } = string.Empty;
    public string Environments { get; set; } = string.Empty;
    public bool Deleted { get; set; }
}
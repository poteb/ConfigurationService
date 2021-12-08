namespace pote.Config.Admin.Api.Model;

public class Configuration
{
    public string Gid { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Json { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public string Integrations { get; set; } = string.Empty;
    public bool Deleted { get; set; }
}
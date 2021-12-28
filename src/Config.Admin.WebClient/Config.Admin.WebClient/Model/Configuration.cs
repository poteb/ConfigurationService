namespace pote.Config.Admin.WebClient.Model;

public class Configuration
{
    public string Gid { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Json { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public bool Deleted { get; set; }

    public List<ConfigEnvironment> Environments { get; set; } = new();
    public List<ConfigSystem> Systems { get; set; } = new();
    public List<Configuration> History { get; set; } = new();
}
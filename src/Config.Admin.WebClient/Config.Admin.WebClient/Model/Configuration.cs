namespace pote.Config.Admin.WebClient.Model;

public class Configuration
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
}

public class ConfigurationHeader
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdateUtc { get; set; } = DateTime.UtcNow;
    public bool Deleted { get; set; }
    public bool IsActive { get; set; }
    public List<Configuration> Configurations { get; set; } = new();
}
namespace pote.Config.DbModel;

public class Configuration
{
    public string HeaderId { get; set; } = string.Empty;
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Json { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public string Applications { get; set; } = string.Empty;
    public string Environments { get; set; } = string.Empty;
    public bool Deleted { get; set; }
    public List<Configuration> History { get; set; } = new();
}
namespace pote.Config.Admin.WebClient.Model;

public class ConfigSystem
{
    public string  Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
}
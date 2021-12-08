namespace Config.Admin.WebClient.Model;

public class Environment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
}
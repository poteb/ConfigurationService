namespace Config.Admin.WebClient.Model;

public class System
{
    public string  Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
}
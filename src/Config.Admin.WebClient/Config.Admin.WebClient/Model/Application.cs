namespace pote.Config.Admin.WebClient.Model;

public class Application
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public bool IsSelected { get; set; }

    public override string ToString()
    {
        return Name;
    }
}
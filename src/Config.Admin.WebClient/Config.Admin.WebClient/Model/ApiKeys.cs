namespace pote.Config.Admin.WebClient.Model;

public class ApiKeys
{
    public List<ApiKey> Keys { get; set; } = new();
}

public class ApiKey
{
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}
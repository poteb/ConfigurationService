namespace pote.Config.Admin.WebClient.Model;

public class ApiKeys
{
    public List<ApiKey> Keys { get; set; } = new();
}

public class ApiKey
{
    public string Key { get; set; }
}
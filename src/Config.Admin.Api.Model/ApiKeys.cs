namespace pote.Config.Admin.Api.Model;

public class ApiKeys
{
    public List<ApiKeyEntry> Keys { get; set; } = new();
}

public class ApiKeyEntry
{
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}
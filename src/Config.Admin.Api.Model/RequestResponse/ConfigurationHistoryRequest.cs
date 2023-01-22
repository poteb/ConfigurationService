namespace pote.Config.Admin.Api.Model.RequestResponse;

public class ConfigurationHistoryRequest
{
    public string Id { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class ConfigurationHistoryResponse
{
    public List<ConfigurationHeader> History { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
}
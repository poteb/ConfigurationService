namespace pote.Config.Admin.Api.Model.RequestResponse;

public class ConfigurationHistoryResponse
{
    public string HeaderId { get; set; } = string.Empty;
    public List<Configuration> History { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
}
namespace pote.Config.Admin.Api.Model.RequestResponse;

public class HeaderHistoryResponse
{
    public List<ConfigurationHeader> History { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
}
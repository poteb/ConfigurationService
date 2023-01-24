namespace pote.Config.Admin.Api.Model.RequestResponse;

public class HeaderHistoryRequest
{
    public string Id { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
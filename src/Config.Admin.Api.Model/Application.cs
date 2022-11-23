namespace pote.Config.Admin.Api.Model;

public class Application : IIdentity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
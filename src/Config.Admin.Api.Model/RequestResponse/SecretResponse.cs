namespace pote.Config.Admin.Api.Model.RequestResponse;

public class SecretResponse
{
    public SecretHeader Secret { get; set; } = new();
    public List<Secret> History { get; set; } = new();
}
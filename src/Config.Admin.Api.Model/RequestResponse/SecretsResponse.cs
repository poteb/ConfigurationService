namespace pote.Config.Admin.Api.Model.RequestResponse;

public class SecretsResponse
{
    public List<SecretHeader> Secrets { get; set; } = new();
}
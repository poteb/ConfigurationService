namespace pote.Config.Middleware.Secrets;

public interface ISecretSettings
{
    ISecretResolver SecretResolver { get; set; }
}
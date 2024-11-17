namespace pote.Config.Shared.Secrets;

public interface ISecretSettings
{
    ISecretResolver SecretResolver { get; set; }
}

public abstract class SecretSettings : ISecretSettings
{
    public ISecretResolver SecretResolver { get; set; }
    
    public SecretSettings(ISecretResolver secretResolver)
    {
        SecretResolver = secretResolver;
    }
}
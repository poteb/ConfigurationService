namespace pote.Config.Shared;

public class SecretValueRequest
{
    public string SecretName { get; set; }
    public string Application { get; set; }
    public string Environment { get; set; }
}
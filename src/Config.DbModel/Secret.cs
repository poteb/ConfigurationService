namespace pote.Config.DbModel;

public class Secret
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
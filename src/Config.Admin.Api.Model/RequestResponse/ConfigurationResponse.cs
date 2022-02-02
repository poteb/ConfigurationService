namespace pote.Config.Admin.Api.Model.RequestResponse;

public class ConfigurationResponse
{
    public ConfigurationHeader Configuration { get; set; } = new();
    public List<Configuration> History { get; set; } = new();
}
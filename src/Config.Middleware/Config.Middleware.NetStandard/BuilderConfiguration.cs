namespace pote.Config.Middleware;

public class BuilderConfiguration
{
    public string RootApiUri { get; set; } = "";
    public string Application { get; set; } = "";
    public string Environment { get; set; } = "";
    public string WorkingDirectory { get; set; } = "";
}
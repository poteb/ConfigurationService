using System.IO;

namespace pote.Config.Middleware;

public class BuilderConfiguration
{
    public string RootApiUri { get; set; } = "";
    public string Application { get; set; } = "";
    public string Environment { get; set; } = "";
    public string WorkingDirectory { get; set; } = "";

    /// <summary>
    /// Enable persistent configuration cache that survives deployments.
    /// When enabled, resolved configuration is also saved outside the deployment directory.
    /// </summary>
    public bool EnablePersistentCache { get; set; } = true;

    /// <summary>
    /// Base directory for persistent cache.
    /// Default: C:\ProgramData\pote\ConfigurationCache
    /// </summary>
    public string PersistentCacheBaseDirectory { get; set; } =
        Path.Combine(System.Environment.GetFolderPath(
            System.Environment.SpecialFolder.CommonApplicationData),
            "pote", "ConfigurationCache");
}
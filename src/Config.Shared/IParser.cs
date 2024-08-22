namespace pote.Config.Shared
{
    public interface IParser
    {
        Action<string, string> TrackingAction { get; set; }
        
        Task<string> Parse(string json, string application, string environment, Action<string> problems, CancellationToken cancellationToken, string encryptionKey, bool resolveSecrets, string rootId = "");
    }
}
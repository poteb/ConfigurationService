using System;

namespace pote.Config.Shared
{
    public interface IParser
    {
        Task<string> Parse(string json, string application, string environment, Action<string> problems, CancellationToken cancellationToken);
    }
}
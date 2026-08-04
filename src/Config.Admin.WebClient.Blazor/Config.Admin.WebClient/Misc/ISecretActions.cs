using pote.Config.Admin.WebClient.Model;

namespace pote.Config.Admin.WebClient.Misc;

public interface ISecretActions
{
    void DuplicateSecret(Secret secret);
}
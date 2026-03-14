using pote.Config.Admin.WebClient.Model;

namespace pote.Config.Admin.WebClient.Misc;

public interface IConfigurationActions
{
    void DuplicateConfiguration(Configuration configuration);
    void NavigateToReference(string configName, bool openInNewTab = false);
}
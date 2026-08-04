namespace pote.Config.Admin.WebClient.Mappers;

public class SettingsMapper
{
    public static Model.Settings ToClient(Api.Model.Settings settings)
    {
        return new Model.Settings
        {
            EncryptAllJson = settings.EncryptAllJson
        };
    }
    
    public static Api.Model.Settings ToApi(Model.Settings settings)
    {
        return new Api.Model.Settings
        {
            EncryptAllJson = settings.EncryptAllJson
        };
    }
}
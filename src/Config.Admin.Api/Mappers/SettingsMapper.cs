namespace pote.Config.Admin.Api.Mappers;

public static class SettingsMapper
{
    public static Model.Settings ToApi(DbModel.Settings settings)
    {
        return new Model.Settings
        {
            EncryptAllJson = settings.EncryptAllJson
        };
    }
    
    public static DbModel.Settings ToDb(Model.Settings settings)
    {
        return new DbModel.Settings
        {
            EncryptAllJson = settings.EncryptAllJson
        };
    }
}
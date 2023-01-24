using Newtonsoft.Json;

namespace pote.Config.Admin.WebClient.Misc;

public static class FormatJsonHelper
{
    public static string FormatJson(string json)
    {
        try
        {
            dynamic parsedJson = JsonConvert.DeserializeObject(json)!;
            if (parsedJson == null) return "Unable to parse json";
            return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
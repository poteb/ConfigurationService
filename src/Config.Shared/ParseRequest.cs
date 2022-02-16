using System.Text;

namespace pote.Config.Shared;

public class ParseRequest
{
    public byte[] InputConfiguration { get; set; }
    public string Application { get; set; }
    public string Environment { get; set; }

    public string AsJson()
    {
        return Encoding.ASCII.GetString(InputConfiguration);
    }
}
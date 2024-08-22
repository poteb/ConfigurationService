using System.Text;

namespace pote.Config.Shared;

public class ParseRequest
{
    public byte[] InputJson { get; set; }
    public string Application { get; set; }
    public string Environment { get; set; }
    public bool ResolveSecrets { get; set; } = false;

    public ParseRequest()
    {
        
    }
    
    public ParseRequest(string application, string environment, string inputJson)
    {
        Application = application;
        Environment = environment;
        InputJson = Encoding.ASCII.GetBytes(inputJson);
    }
    
    public string AsJson()
    {
        return Encoding.ASCII.GetString(InputJson);
    }
}
using System.Text;

namespace pote.Config.Shared;

public class ParseResponse
{
    public byte[] OutputConfiguration { get; set; } = null!;
    public string Application { get; set; } = null!;
    public string Environment { get; set; } = null!;
    public List<string> Problems { get; set; } = new();

    public void AddProblem(string problem)
    {
        Problems.Add(problem);
    }

    public void FromJson(string json)
    {
        OutputConfiguration = Encoding.ASCII.GetBytes(json);
    }

    public string GetJson()
    {
        if (OutputConfiguration == null) return string.Empty; 
        return Encoding.ASCII.GetString(OutputConfiguration);
    }
}
using System.Text;

namespace pote.Config.Shared;

public class ParseResponse
{
    public byte[] OutputJson { get; set; } = null!;
    public string Application { get; set; } = null!;
    public string Environment { get; set; } = null!;
    public List<string> Problems { get; set; } = new();

    public void AddProblem(string problem)
    {
        Problems.Add(problem);
    }

    public void FromJson(string json)
    {
        OutputJson = Encoding.ASCII.GetBytes(json);
    }

    public string GetJson()
    {
        if (OutputJson == null) return string.Empty; 
        return Encoding.ASCII.GetString(OutputJson);
    }
}
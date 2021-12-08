using System.Text;

namespace pote.Config.Shared;

public class ParseResponse
{
    public byte[] OutputConfiguration { get; set; }
    public string System { get; set; }
    public string Environment { get; set; }
    public List<string> Problems { get; set; } = new List<string>();

    public void AddProblem(string problem)
    {
        Problems.Add(problem);
    }

    public void FromJson(string json)
    {
        OutputConfiguration = Encoding.ASCII.GetBytes(json);
    }
}
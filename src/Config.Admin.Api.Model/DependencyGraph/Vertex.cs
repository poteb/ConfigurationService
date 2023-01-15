namespace pote.Config.Admin.Api.Model.DependencyGraph;

public record Vertex(string Id, string Name, string Type)
 {
//     public string Id { get; set; }
//     public string Type { get; set; }
//
     public List<string> Edges { get; } = new();
//     public string Name { get; }
//     
//     public Vertex(string id, string name)
//     {
//         Name = name;
//         Id = id;
//     }
 }
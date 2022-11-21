// namespace pote.Config.Admin.Api.Model.DependencyGraph;
//
// /// <summary>A node representing a <see cref="Configuration"/> in the dependency graph</summary>
// public class ConfigurationVertex : Vertex<Configuration>
// {
//     private readonly ConfigurationHeader _configurationHeader;
//     
//     public override string Name => $"{_configurationHeader.Name} ({_configurationHeader.Id})";
//     
//     public ConfigurationVertex(Configuration configuration, ConfigurationHeader configurationHeader) : base(configuration)
//     {
//         _configurationHeader = configurationHeader;
//     }
// }
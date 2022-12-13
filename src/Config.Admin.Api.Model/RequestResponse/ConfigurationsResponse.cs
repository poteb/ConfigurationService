using System.Collections.Immutable;

namespace pote.Config.Admin.Api.Model.RequestResponse;

public class ConfigurationsResponse
{
    public IReadOnlyList<ConfigurationHeader> Configurations { get; set; } = ImmutableList<ConfigurationHeader>.Empty;
}
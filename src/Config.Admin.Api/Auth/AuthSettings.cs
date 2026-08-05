namespace pote.Config.Admin.Api.Auth;

public class AuthSettings
{
    /// <summary>Active auth provider. Only "Local" is implemented.</summary>
    public string Provider { get; set; } = "Local";
    public int SessionLifetimeHours { get; set; } = 8;
    /// <summary>Lifetime for invite links and reset links.</summary>
    public int InviteLifetimeDays { get; set; } = 7;
}

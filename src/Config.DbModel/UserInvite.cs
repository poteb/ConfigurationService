namespace pote.Config.DbModel;

public class UserInvite
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = UserRoles.User;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime ExpiresUtc { get; set; }
}

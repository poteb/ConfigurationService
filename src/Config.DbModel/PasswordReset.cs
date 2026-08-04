namespace pote.Config.DbModel;

public class PasswordReset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime ExpiresUtc { get; set; }
}

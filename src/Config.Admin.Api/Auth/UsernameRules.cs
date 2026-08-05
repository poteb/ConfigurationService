namespace pote.Config.Admin.Api.Auth;

public static class UsernameRules
{
    public const string GuestUsername = "guest";
    public const int MaxLength = 100;

    /// <summary>
    /// Null (with the trimmed username in the out parameter) when valid,
    /// otherwise a human-readable reason. Allowed characters: letters, digits, . - _ @
    /// — conservative on purpose: usernames appear in route segments and logs.
    /// </summary>
    public static string? Validate(string raw, out string trimmed)
    {
        trimmed = (raw ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            return "Username is required.";
        if (trimmed.Length > MaxLength)
            return $"Username must be at most {MaxLength} characters.";
        if (!trimmed.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '_' or '@'))
            return "Username may only contain letters, digits, and . - _ @";
        if (trimmed.Equals(GuestUsername, StringComparison.OrdinalIgnoreCase))
            return "That username is reserved.";
        return null;
    }
}

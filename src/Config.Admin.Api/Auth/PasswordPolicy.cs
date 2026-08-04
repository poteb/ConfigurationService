namespace pote.Config.Admin.Api.Auth;

public static class PasswordPolicy
{
    public const int MinLength = 16;
    public const int MaxLength = 128;

    /// <summary>Null when valid, otherwise a human-readable reason.</summary>
    public static string? Validate(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
            return $"Password must be at least {MinLength} characters.";
        if (password.Length > MaxLength)
            return $"Password must be at most {MaxLength} characters.";
        if (!password.Any(char.IsLower))
            return "Password must contain a lowercase letter.";
        if (!password.Any(char.IsUpper))
            return "Password must contain an uppercase letter.";
        if (!password.Any(char.IsDigit))
            return "Password must contain a digit.";
        if (password.All(char.IsLetterOrDigit))
            return "Password must contain a special character.";
        return null;
    }
}

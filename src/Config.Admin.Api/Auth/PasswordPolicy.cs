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
        // Explicit ASCII classes to stay in lockstep with the SPA's regex checks
        // (Unicode-aware char.IsLower etc. would accept passwords the client rejects).
        if (!password.Any(char.IsAsciiLetterLower))
            return "Password must contain a lowercase letter.";
        if (!password.Any(char.IsAsciiLetterUpper))
            return "Password must contain an uppercase letter.";
        if (!password.Any(char.IsAsciiDigit))
            return "Password must contain a digit.";
        if (password.All(char.IsAsciiLetterOrDigit))
            return "Password must contain a special character.";
        return null;
    }
}

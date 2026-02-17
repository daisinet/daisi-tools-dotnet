namespace SecureToolProvider.Common.Models;

/// <summary>
/// Stores OAuth tokens for a user's authenticated session.
/// Serialized to JSON and stored in the setup store.
/// </summary>
public class OAuthTokens
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public string? TokenType { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string? Scope { get; set; }

    /// <summary>
    /// Whether the access token has expired (with a 5-minute buffer).
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt.AddMinutes(-5);
}

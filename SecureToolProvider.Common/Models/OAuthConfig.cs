namespace SecureToolProvider.Common.Models;

/// <summary>
/// Configuration for an OAuth 2.0 provider (Google, Microsoft, etc.).
/// </summary>
public class OAuthConfig
{
    public required string AuthorizeUrl { get; set; }
    public required string TokenUrl { get; set; }
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
    public required string[] Scopes { get; set; }
    public required string RedirectUri { get; set; }

    /// <summary>
    /// Additional query parameters to include in the authorization URL.
    /// For example, Google requires "access_type=offline" for refresh tokens.
    /// </summary>
    public Dictionary<string, string> AdditionalAuthParams { get; set; } = new();
}

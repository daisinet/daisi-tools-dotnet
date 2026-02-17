using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Daisi.SecureTools.Microsoft365;

/// <summary>
/// Factory that creates a <see cref="GraphServiceClient"/> authenticated with a
/// pre-existing OAuth access token. The token is injected as a Bearer header via
/// a custom <see cref="IAccessTokenProvider"/>.
/// </summary>
public class GraphClientFactory
{
    /// <summary>
    /// Create a <see cref="GraphServiceClient"/> that authenticates every request
    /// with the supplied access token.
    /// </summary>
    public GraphServiceClient CreateClient(string accessToken)
    {
        var tokenProvider = new StaticAccessTokenProvider(accessToken);
        var authProvider = new BaseBearerTokenAuthenticationProvider(tokenProvider);
        return new GraphServiceClient(authProvider);
    }

    /// <summary>
    /// Simple token provider that always returns a fixed access token.
    /// Used to bridge our stored OAuth token into the Microsoft Graph SDK.
    /// </summary>
    private sealed class StaticAccessTokenProvider(string accessToken) : IAccessTokenProvider
    {
        public AllowedHostsValidator AllowedHostsValidator { get; } = new();

        public Task<string> GetAuthorizationTokenAsync(
            Uri uri,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(accessToken);
        }
    }
}

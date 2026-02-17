using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SecureToolProvider.Common.Models;

namespace SecureToolProvider.Common.Tests;

public class OAuthHelperTests
{
    private static OAuthConfig CreateTestConfig() => new()
    {
        AuthorizeUrl = "https://accounts.google.com/o/oauth2/v2/auth",
        TokenUrl = "https://oauth2.googleapis.com/token",
        ClientId = "test-client-id",
        ClientSecret = "test-client-secret",
        Scopes = ["email", "profile"],
        RedirectUri = "https://example.com/api/auth/callback",
        AdditionalAuthParams = new() { ["access_type"] = "offline" }
    };

    [Fact]
    public void BuildAuthorizeUrl_ContainsRequiredParams()
    {
        var config = CreateTestConfig();
        var helper = new OAuthHelper(config, new HttpClient(), NullLogger.Instance);

        var (url, state) = helper.BuildAuthorizeUrl("inst-1", "google");

        Assert.Contains("client_id=test-client-id", url);
        Assert.Contains("redirect_uri=", url);
        Assert.Contains("response_type=code", url);
        Assert.Contains("scope=email%20profile", url);
        Assert.Contains("code_challenge=", url);
        Assert.Contains("code_challenge_method=S256", url);
        Assert.Contains("access_type=offline", url);
        Assert.NotEmpty(state);
    }

    [Fact]
    public void BuildAuthorizeUrl_StartsWithAuthorizeEndpoint()
    {
        var config = CreateTestConfig();
        var helper = new OAuthHelper(config, new HttpClient(), NullLogger.Instance);

        var (url, _) = helper.BuildAuthorizeUrl("inst-1", "google");

        Assert.StartsWith("https://accounts.google.com/o/oauth2/v2/auth?", url);
    }

    [Fact]
    public void ParseState_ExtractsInstallIdAndSetupKey()
    {
        var config = CreateTestConfig();
        var helper = new OAuthHelper(config, new HttpClient(), NullLogger.Instance);

        var (_, state) = helper.BuildAuthorizeUrl("inst-42", "mykey");
        var parsed = OAuthHelper.ParseState(state);

        Assert.NotNull(parsed);
        Assert.Equal("inst-42", parsed.Value.InstallId);
        Assert.Equal("mykey", parsed.Value.SetupKey);
    }

    [Fact]
    public void ParseState_ReturnsNullForInvalidState()
    {
        Assert.Null(OAuthHelper.ParseState("not-valid-base64!!!"));
    }

    [Fact]
    public async Task ExchangeCodeForTokens_ReturnsTokensOnSuccess()
    {
        var tokenResponse = JsonSerializer.Serialize(new
        {
            access_token = "test-access-token",
            refresh_token = "test-refresh-token",
            token_type = "Bearer",
            expires_in = 3600,
            scope = "email profile"
        });

        var handler = new MockHttpHandler(tokenResponse, HttpStatusCode.OK);
        var config = CreateTestConfig();
        var helper = new OAuthHelper(config, new HttpClient(handler), NullLogger.Instance);

        // Build URL first to store PKCE verifier
        var (_, state) = helper.BuildAuthorizeUrl("inst-1", "google");
        var tokens = await helper.ExchangeCodeForTokensAsync("auth-code-123", state);

        Assert.NotNull(tokens);
        Assert.Equal("test-access-token", tokens.AccessToken);
        Assert.Equal("test-refresh-token", tokens.RefreshToken);
        Assert.Equal("Bearer", tokens.TokenType);
        Assert.False(tokens.IsExpired);
    }

    [Fact]
    public async Task ExchangeCodeForTokens_ReturnsNullOnFailure()
    {
        var handler = new MockHttpHandler("error", HttpStatusCode.BadRequest);
        var config = CreateTestConfig();
        var helper = new OAuthHelper(config, new HttpClient(handler), NullLogger.Instance);

        var (_, state) = helper.BuildAuthorizeUrl("inst-1", "google");
        var tokens = await helper.ExchangeCodeForTokensAsync("bad-code", state);

        Assert.Null(tokens);
    }

    [Fact]
    public async Task RefreshTokens_ReturnsNewTokens()
    {
        var tokenResponse = JsonSerializer.Serialize(new
        {
            access_token = "refreshed-access-token",
            token_type = "Bearer",
            expires_in = 3600
        });

        var handler = new MockHttpHandler(tokenResponse, HttpStatusCode.OK);
        var config = CreateTestConfig();
        var helper = new OAuthHelper(config, new HttpClient(handler), NullLogger.Instance);

        var tokens = await helper.RefreshTokensAsync("old-refresh-token");

        Assert.NotNull(tokens);
        Assert.Equal("refreshed-access-token", tokens.AccessToken);
    }

    [Fact]
    public void OAuthTokens_IsExpired_TrueWhenExpired()
    {
        var tokens = new OAuthTokens
        {
            AccessToken = "test",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        Assert.True(tokens.IsExpired);
    }

    [Fact]
    public void OAuthTokens_IsExpired_TrueWithinBuffer()
    {
        var tokens = new OAuthTokens
        {
            AccessToken = "test",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(3) // within 5-min buffer
        };
        Assert.True(tokens.IsExpired);
    }

    [Fact]
    public void OAuthTokens_IsExpired_FalseWhenFresh()
    {
        var tokens = new OAuthTokens
        {
            AccessToken = "test",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        Assert.False(tokens.IsExpired);
    }

    /// <summary>
    /// Simple mock HTTP handler for testing token exchange requests.
    /// </summary>
    private class MockHttpHandler(string responseContent, HttpStatusCode statusCode) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseContent, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}

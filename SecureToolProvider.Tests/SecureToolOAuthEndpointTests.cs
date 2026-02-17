using System.Net;
using Microsoft.Extensions.Logging;
using SecureToolProvider.Tests.Helpers;

namespace SecureToolProvider.Tests;

/// <summary>
/// Integration/smoke tests for the three OAuth Azure Function endpoints.
/// Tests the full request-response cycle through the function classes.
/// </summary>
public class SecureToolOAuthEndpointTests
{
    private readonly SetupStore _store;
    private readonly SecureToolFunctions _functions;

    public SecureToolOAuthEndpointTests()
    {
        _store = new SetupStore();
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<SecureToolFunctions>();
        _functions = new SecureToolFunctions(logger, _store);
    }

    #region POST /auth/status

    [Fact]
    public async Task AuthStatus_RegisteredInstall_NotConnected_ReturnsFalse()
    {
        _store.RegisterInstall("inst-1", "tool-1");

        var request = TestHelpers.CreatePostRequest(
            "http://localhost/api/auth/status",
            new AuthStatusRequest { InstallId = "inst-1", Service = "office365" });

        var response = await _functions.AuthStatus(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await TestHelpers.ReadResponseAsync<AuthStatusResponse>(response);
        Assert.NotNull(body);
        Assert.False(body.Connected);
        Assert.Equal("office365", body.ServiceName);
    }

    [Fact]
    public async Task AuthStatus_RegisteredInstall_Connected_ReturnsTrue()
    {
        _store.RegisterInstall("inst-1", "tool-1");
        _store.SaveOAuthTokens("inst-1", "office365", "access", "refresh", DateTime.UtcNow.AddHours(1));

        var request = TestHelpers.CreatePostRequest(
            "http://localhost/api/auth/status",
            new AuthStatusRequest { InstallId = "inst-1", Service = "office365" });

        var response = await _functions.AuthStatus(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await TestHelpers.ReadResponseAsync<AuthStatusResponse>(response);
        Assert.NotNull(body);
        Assert.True(body.Connected);
    }

    [Fact]
    public async Task AuthStatus_UnknownInstall_ReturnsForbidden()
    {
        var request = TestHelpers.CreatePostRequest(
            "http://localhost/api/auth/status",
            new AuthStatusRequest { InstallId = "unknown-inst", Service = "office365" });

        var response = await _functions.AuthStatus(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AuthStatus_EmptyInstallId_ReturnsBadRequest()
    {
        var request = TestHelpers.CreatePostRequest(
            "http://localhost/api/auth/status",
            new AuthStatusRequest { InstallId = "", Service = "office365" });

        var response = await _functions.AuthStatus(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AuthStatus_MultipleServices_ReportsEachIndependently()
    {
        _store.RegisterInstall("inst-1", "tool-1");
        _store.SaveOAuthTokens("inst-1", "office365", "at", "rt", DateTime.UtcNow.AddHours(1));
        // google is NOT connected

        var o365Req = TestHelpers.CreatePostRequest(
            "http://localhost/api/auth/status",
            new AuthStatusRequest { InstallId = "inst-1", Service = "office365" });
        var googleReq = TestHelpers.CreatePostRequest(
            "http://localhost/api/auth/status",
            new AuthStatusRequest { InstallId = "inst-1", Service = "google" });

        var o365Resp = await _functions.AuthStatus(o365Req);
        var googleResp = await _functions.AuthStatus(googleReq);

        var o365Body = await TestHelpers.ReadResponseAsync<AuthStatusResponse>(o365Resp);
        var googleBody = await TestHelpers.ReadResponseAsync<AuthStatusResponse>(googleResp);

        Assert.True(o365Body!.Connected);
        Assert.False(googleBody!.Connected);
    }

    #endregion

    #region GET /auth/start

    [Fact]
    public void AuthStart_ValidParams_RedirectsToCallback()
    {
        var request = TestHelpers.CreateGetRequest(
            "http://localhost/api/auth/start?installId=inst-1&returnUrl=http://manager/oauth-callback&service=office365");

        var response = _functions.AuthStart(request);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Location", out var locations));
        var location = locations.First();
        Assert.Contains("/api/auth/callback", location);
        Assert.Contains("code=simulated-auth-code", location);
        Assert.Contains("state=", location);
    }

    [Fact]
    public void AuthStart_MissingInstallId_ReturnsBadRequest()
    {
        var request = TestHelpers.CreateGetRequest(
            "http://localhost/api/auth/start?returnUrl=http://manager/oauth-callback&service=office365");

        var response = _functions.AuthStart(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void AuthStart_MissingReturnUrl_ReturnsBadRequest()
    {
        var request = TestHelpers.CreateGetRequest(
            "http://localhost/api/auth/start?installId=inst-1&service=office365");

        var response = _functions.AuthStart(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void AuthStart_StateEncodesAllParams()
    {
        var request = TestHelpers.CreateGetRequest(
            "http://localhost/api/auth/start?installId=my-install&returnUrl=http://manager/callback&service=google");

        var response = _functions.AuthStart(request);
        response.Headers.TryGetValues("Location", out var locations);
        var location = locations!.First();

        // Extract state parameter from redirect URL
        var uri = new Uri(location);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var stateEncoded = query["state"]!;
        var stateJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(stateEncoded));

        Assert.Contains("my-install", stateJson);
        Assert.Contains("http://manager/callback", stateJson);
        Assert.Contains("google", stateJson);
    }

    #endregion

    #region GET /auth/callback

    [Fact]
    public void AuthCallback_ValidCodeAndState_StoresTokensAndRedirects()
    {
        _store.RegisterInstall("inst-1", "tool-1");

        // Build state as auth/start would
        var stateJson = System.Text.Json.JsonSerializer.Serialize(
            new { installId = "inst-1", returnUrl = "http://manager/oauth-callback", service = "office365" });
        var state = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(stateJson));

        var request = TestHelpers.CreateGetRequest(
            $"http://localhost/api/auth/callback?code=auth-code-123&state={Uri.EscapeDataString(state)}");

        var response = _functions.AuthCallback(request);

        // Should redirect to the returnUrl
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Location", out var locations));
        Assert.Equal("http://manager/oauth-callback", locations.First());

        // Tokens should be stored
        Assert.True(_store.IsOAuthConnected("inst-1", "office365"));
        var tokens = _store.GetOAuthTokens("inst-1", "office365");
        Assert.NotNull(tokens);
        Assert.StartsWith("simulated-access-token-", tokens.AccessToken);
        Assert.StartsWith("simulated-refresh-token-", tokens.RefreshToken);
        Assert.True(tokens.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public void AuthCallback_UnknownInstall_ReturnsForbidden()
    {
        var stateJson = System.Text.Json.JsonSerializer.Serialize(
            new { installId = "unknown-inst", returnUrl = "http://manager/callback", service = "office365" });
        var state = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(stateJson));

        var request = TestHelpers.CreateGetRequest(
            $"http://localhost/api/auth/callback?code=auth-code&state={Uri.EscapeDataString(state)}");

        var response = _functions.AuthCallback(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public void AuthCallback_MissingCode_ReturnsBadRequest()
    {
        var request = TestHelpers.CreateGetRequest(
            "http://localhost/api/auth/callback?state=abc");

        var response = _functions.AuthCallback(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void AuthCallback_MissingState_ReturnsBadRequest()
    {
        var request = TestHelpers.CreateGetRequest(
            "http://localhost/api/auth/callback?code=auth-code");

        var response = _functions.AuthCallback(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void AuthCallback_InvalidState_ReturnsBadRequest()
    {
        var request = TestHelpers.CreateGetRequest(
            "http://localhost/api/auth/callback?code=auth-code&state=not-valid-base64!!!");

        var response = _functions.AuthCallback(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Full OAuth Flow (Integration)

    [Fact]
    public async Task FullOAuthFlow_StartToStatusCheck()
    {
        // 1. Register an installation
        _store.RegisterInstall("flow-inst", "tool-1");

        // 2. Verify not connected initially
        var statusReq1 = TestHelpers.CreatePostRequest(
            "http://localhost/api/auth/status",
            new AuthStatusRequest { InstallId = "flow-inst", Service = "office365" });
        var statusResp1 = await _functions.AuthStatus(statusReq1);
        var statusBody1 = await TestHelpers.ReadResponseAsync<AuthStatusResponse>(statusResp1);
        Assert.False(statusBody1!.Connected);

        // 3. Initiate OAuth flow (auth/start)
        var startReq = TestHelpers.CreateGetRequest(
            "http://localhost/api/auth/start?installId=flow-inst&returnUrl=http://manager/oauth-callback&service=office365");
        var startResp = _functions.AuthStart(startReq);
        Assert.Equal(HttpStatusCode.Redirect, startResp.StatusCode);

        // 4. Follow the redirect to auth/callback
        startResp.Headers.TryGetValues("Location", out var callbackLocations);
        var callbackUrl = callbackLocations!.First();
        var callbackReq = TestHelpers.CreateGetRequest(callbackUrl);
        var callbackResp = _functions.AuthCallback(callbackReq);
        Assert.Equal(HttpStatusCode.Redirect, callbackResp.StatusCode);

        // Verify callback redirects to the returnUrl
        callbackResp.Headers.TryGetValues("Location", out var returnLocations);
        Assert.Equal("http://manager/oauth-callback", returnLocations!.First());

        // 5. Verify now connected
        var statusReq2 = TestHelpers.CreatePostRequest(
            "http://localhost/api/auth/status",
            new AuthStatusRequest { InstallId = "flow-inst", Service = "office365" });
        var statusResp2 = await _functions.AuthStatus(statusReq2);
        var statusBody2 = await TestHelpers.ReadResponseAsync<AuthStatusResponse>(statusResp2);
        Assert.True(statusBody2!.Connected);
        Assert.Equal("office365", statusBody2.ServiceName);
    }

    [Fact]
    public async Task Reconnect_OverwritesPreviousTokens()
    {
        _store.RegisterInstall("inst-recon", "tool-1");
        _store.SaveOAuthTokens("inst-recon", "google", "old-access", "old-refresh", DateTime.UtcNow.AddHours(1));

        // Verify connected
        Assert.True(_store.IsOAuthConnected("inst-recon", "google"));
        var oldTokens = _store.GetOAuthTokens("inst-recon", "google");

        // Re-do the OAuth flow (simulating reconnect)
        var stateJson = System.Text.Json.JsonSerializer.Serialize(
            new { installId = "inst-recon", returnUrl = "http://manager/callback", service = "google" });
        var state = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(stateJson));
        var callbackReq = TestHelpers.CreateGetRequest(
            $"http://localhost/api/auth/callback?code=new-code&state={Uri.EscapeDataString(state)}");
        _functions.AuthCallback(callbackReq);

        // Verify tokens were overwritten
        var newTokens = _store.GetOAuthTokens("inst-recon", "google");
        Assert.NotNull(newTokens);
        Assert.NotEqual(oldTokens!.AccessToken, newTokens.AccessToken);
    }

    [Fact]
    public async Task Uninstall_ClearsOAuthTokens_StatusReturnsNotConnected()
    {
        // Setup: install + OAuth connect
        _store.RegisterInstall("inst-unsub", "tool-1");
        _store.SaveOAuthTokens("inst-unsub", "office365", "at", "rt", DateTime.UtcNow.AddHours(1));
        Assert.True(_store.IsOAuthConnected("inst-unsub", "office365"));

        // Uninstall
        _store.RemoveInstall("inst-unsub");

        // Auth status should now return forbidden (unknown install)
        var statusReq = TestHelpers.CreatePostRequest(
            "http://localhost/api/auth/status",
            new AuthStatusRequest { InstallId = "inst-unsub", Service = "office365" });
        var statusResp = await _functions.AuthStatus(statusReq);
        Assert.Equal(HttpStatusCode.Forbidden, statusResp.StatusCode);
    }

    #endregion
}

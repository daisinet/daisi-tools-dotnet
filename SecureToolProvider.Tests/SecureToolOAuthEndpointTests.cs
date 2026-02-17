namespace SecureToolProvider.Tests;

/// <summary>
/// Tests the full bundle OAuth flow: install 2 tools with the same bundleInstallId,
/// OAuth from tool A, verify tool B sees Connected.
/// </summary>
public class SecureToolOAuthEndpointTests
{
    private readonly SetupStore _store = new();

    [Fact]
    public void BundleFlow_InstallTwoTools_OAuthFromToolA_ToolBSeesConnected()
    {
        // Simulate ORC sending /install for two bundled tools
        _store.RegisterInstall("inst-tool-a", "calendar-tool", "binst-shared");
        _store.RegisterInstall("inst-tool-b", "email-tool", "binst-shared");

        // Both tools should be installed
        Assert.True(_store.IsInstalled("inst-tool-a"));
        Assert.True(_store.IsInstalled("inst-tool-b"));

        // Neither tool has OAuth tokens yet
        Assert.False(_store.HasOAuthTokens("inst-tool-a", "google"));
        Assert.False(_store.HasOAuthTokens("inst-tool-b", "google"));

        // User completes OAuth from tool A's configure page
        _store.SaveOAuthTokens("inst-tool-a", "google", new Dictionary<string, string>
        {
            ["access_token"] = "ya29.google-token",
            ["refresh_token"] = "1//google-refresh"
        });

        // Tool A should see Connected
        Assert.True(_store.HasOAuthTokens("inst-tool-a", "google"));
        var toolATokens = _store.GetOAuthTokens("inst-tool-a", "google");
        Assert.NotNull(toolATokens);
        Assert.Equal("ya29.google-token", toolATokens["access_token"]);

        // Tool B should ALSO see Connected (shared via bundleInstallId)
        Assert.True(_store.HasOAuthTokens("inst-tool-b", "google"));
        var toolBTokens = _store.GetOAuthTokens("inst-tool-b", "google");
        Assert.NotNull(toolBTokens);
        Assert.Equal("ya29.google-token", toolBTokens["access_token"]);
    }

    [Fact]
    public void BundleFlow_NonOAuthSetup_RemainsPerTool()
    {
        // Install two bundled tools
        _store.RegisterInstall("inst-tool-a", "calendar-tool", "binst-shared");
        _store.RegisterInstall("inst-tool-b", "email-tool", "binst-shared");

        // Each tool gets its own non-OAuth setup (API keys, etc.)
        _store.SaveSetup("inst-tool-a", new Dictionary<string, string>
        {
            ["api_key"] = "key-for-calendar"
        });
        _store.SaveSetup("inst-tool-b", new Dictionary<string, string>
        {
            ["api_key"] = "key-for-email"
        });

        // Setup remains per-tool, not shared
        var setupA = _store.GetSetup("inst-tool-a");
        var setupB = _store.GetSetup("inst-tool-b");

        Assert.NotNull(setupA);
        Assert.NotNull(setupB);
        Assert.Equal("key-for-calendar", setupA["api_key"]);
        Assert.Equal("key-for-email", setupB["api_key"]);
    }

    [Fact]
    public void NonBundledTool_OAuthStaysIsolated()
    {
        // Install two tools WITHOUT a bundle
        _store.RegisterInstall("inst-standalone-a", "tool-x");
        _store.RegisterInstall("inst-standalone-b", "tool-y");

        // OAuth from tool X
        _store.SaveOAuthTokens("inst-standalone-a", "github", new Dictionary<string, string>
        {
            ["access_token"] = "gh_tok_123"
        });

        // Tool X sees tokens, tool Y does NOT
        Assert.True(_store.HasOAuthTokens("inst-standalone-a", "github"));
        Assert.False(_store.HasOAuthTokens("inst-standalone-b", "github"));
    }

    [Fact]
    public void BundleFlow_MultipleOAuthServices_IndependentPerService()
    {
        _store.RegisterInstall("inst-tool-a", "calendar-tool", "binst-shared");
        _store.RegisterInstall("inst-tool-b", "email-tool", "binst-shared");

        // OAuth for Google from tool A
        _store.SaveOAuthTokens("inst-tool-a", "google", new Dictionary<string, string>
        {
            ["access_token"] = "google-tok"
        });

        // OAuth for Microsoft from tool B
        _store.SaveOAuthTokens("inst-tool-b", "microsoft", new Dictionary<string, string>
        {
            ["access_token"] = "ms-tok"
        });

        // Both tools see both services
        Assert.True(_store.HasOAuthTokens("inst-tool-a", "google"));
        Assert.True(_store.HasOAuthTokens("inst-tool-b", "google"));
        Assert.True(_store.HasOAuthTokens("inst-tool-a", "microsoft"));
        Assert.True(_store.HasOAuthTokens("inst-tool-b", "microsoft"));
    }
}

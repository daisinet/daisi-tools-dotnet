namespace SecureToolProvider.Tests;

public class SetupStoreOAuthTests
{
    private readonly SetupStore _store = new();

    [Fact]
    public void RegisterInstall_WithBundleId_StoresBundleMapping()
    {
        _store.RegisterInstall("inst-001", "tool-a", "binst-100");

        Assert.Equal("binst-100", _store.GetBundleInstallId("inst-001"));
    }

    [Fact]
    public void RegisterInstall_WithoutBundleId_ReturnsNull()
    {
        _store.RegisterInstall("inst-001", "tool-a");

        Assert.Null(_store.GetBundleInstallId("inst-001"));
    }

    [Fact]
    public void ResolveOAuthKey_WithBundle_UsesBundleId()
    {
        _store.RegisterInstall("inst-001", "tool-a", "binst-100");

        var key = _store.ResolveOAuthKey("inst-001", "github");

        Assert.Equal("binst-100:github", key);
    }

    [Fact]
    public void ResolveOAuthKey_WithoutBundle_UsesInstallId()
    {
        _store.RegisterInstall("inst-001", "tool-a");

        var key = _store.ResolveOAuthKey("inst-001", "github");

        Assert.Equal("inst-001:github", key);
    }

    [Fact]
    public void SaveOAuthTokens_BundledInstalls_ShareTokens()
    {
        // Two installs sharing the same bundle
        _store.RegisterInstall("inst-001", "tool-a", "binst-100");
        _store.RegisterInstall("inst-002", "tool-b", "binst-100");

        // OAuth from tool A
        var tokens = new Dictionary<string, string>
        {
            ["access_token"] = "tok_abc",
            ["refresh_token"] = "ref_xyz"
        };
        _store.SaveOAuthTokens("inst-001", "github", tokens);

        // Tool B should see the same tokens
        var toolBTokens = _store.GetOAuthTokens("inst-002", "github");
        Assert.NotNull(toolBTokens);
        Assert.Equal("tok_abc", toolBTokens["access_token"]);
        Assert.Equal("ref_xyz", toolBTokens["refresh_token"]);
    }

    [Fact]
    public void HasOAuthTokens_BundledInstall_ReturnsTrueForBothTools()
    {
        _store.RegisterInstall("inst-001", "tool-a", "binst-100");
        _store.RegisterInstall("inst-002", "tool-b", "binst-100");

        _store.SaveOAuthTokens("inst-001", "github", new Dictionary<string, string>
        {
            ["access_token"] = "tok_abc"
        });

        Assert.True(_store.HasOAuthTokens("inst-001", "github"));
        Assert.True(_store.HasOAuthTokens("inst-002", "github"));
    }

    [Fact]
    public void HasOAuthTokens_NonBundledInstall_IndependentTokens()
    {
        _store.RegisterInstall("inst-001", "tool-a");
        _store.RegisterInstall("inst-002", "tool-b");

        _store.SaveOAuthTokens("inst-001", "github", new Dictionary<string, string>
        {
            ["access_token"] = "tok_abc"
        });

        Assert.True(_store.HasOAuthTokens("inst-001", "github"));
        Assert.False(_store.HasOAuthTokens("inst-002", "github"));
    }

    [Fact]
    public void RemoveInstall_ClearsBundleMapping()
    {
        _store.RegisterInstall("inst-001", "tool-a", "binst-100");

        _store.RemoveInstall("inst-001");

        Assert.Null(_store.GetBundleInstallId("inst-001"));
        Assert.False(_store.IsInstalled("inst-001"));
    }

    [Fact]
    public void SaveOAuthTokens_DifferentServices_IndependentPerService()
    {
        _store.RegisterInstall("inst-001", "tool-a", "binst-100");

        _store.SaveOAuthTokens("inst-001", "github", new Dictionary<string, string>
        {
            ["access_token"] = "gh_tok"
        });
        _store.SaveOAuthTokens("inst-001", "google", new Dictionary<string, string>
        {
            ["access_token"] = "goog_tok"
        });

        var ghTokens = _store.GetOAuthTokens("inst-001", "github");
        var googTokens = _store.GetOAuthTokens("inst-001", "google");

        Assert.NotNull(ghTokens);
        Assert.NotNull(googTokens);
        Assert.Equal("gh_tok", ghTokens["access_token"]);
        Assert.Equal("goog_tok", googTokens["access_token"]);
    }

    [Fact]
    public void GetOAuthTokens_NoTokensSaved_ReturnsNull()
    {
        _store.RegisterInstall("inst-001", "tool-a", "binst-100");

        Assert.Null(_store.GetOAuthTokens("inst-001", "github"));
    }
}

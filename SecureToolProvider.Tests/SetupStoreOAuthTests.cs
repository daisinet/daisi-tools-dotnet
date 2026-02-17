namespace SecureToolProvider.Tests;

public class SetupStoreOAuthTests
{
    private readonly SetupStore _store = new();

    #region Bundle Registration

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

    #endregion

    #region ResolveOAuthKey

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

    #endregion

    #region SaveOAuthTokens / GetOAuthTokens

    [Fact]
    public void SaveOAuthTokens_StoresTokensSuccessfully()
    {
        _store.RegisterInstall("inst-1", "tool-1");

        _store.SaveOAuthTokens("inst-1", "office365", new Dictionary<string, string>
        {
            ["access_token"] = "access-token",
            ["refresh_token"] = "refresh-token"
        });

        var tokens = _store.GetOAuthTokens("inst-1", "office365");
        Assert.NotNull(tokens);
        Assert.Equal("access-token", tokens["access_token"]);
        Assert.Equal("refresh-token", tokens["refresh_token"]);
    }

    [Fact]
    public void GetOAuthTokens_UnknownService_ReturnsNull()
    {
        _store.RegisterInstall("inst-1", "tool-1");

        var tokens = _store.GetOAuthTokens("inst-1", "nonexistent");

        Assert.Null(tokens);
    }

    [Fact]
    public void GetOAuthTokens_NoTokensSaved_ReturnsNull()
    {
        _store.RegisterInstall("inst-001", "tool-a", "binst-100");

        Assert.Null(_store.GetOAuthTokens("inst-001", "github"));
    }

    [Fact]
    public void SaveOAuthTokens_OverwritesExisting()
    {
        _store.RegisterInstall("inst-1", "tool-1");
        _store.SaveOAuthTokens("inst-1", "office365", new Dictionary<string, string>
        {
            ["access_token"] = "old-token",
            ["refresh_token"] = "old-refresh"
        });

        _store.SaveOAuthTokens("inst-1", "office365", new Dictionary<string, string>
        {
            ["access_token"] = "new-token",
            ["refresh_token"] = "new-refresh"
        });

        var tokens = _store.GetOAuthTokens("inst-1", "office365");
        Assert.NotNull(tokens);
        Assert.Equal("new-token", tokens["access_token"]);
        Assert.Equal("new-refresh", tokens["refresh_token"]);
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
    public void MultipleServices_SameInstall_IndependentlyTracked()
    {
        _store.RegisterInstall("inst-1", "tool-1");
        _store.SaveOAuthTokens("inst-1", "office365", new Dictionary<string, string>
        {
            ["access_token"] = "at-o365",
            ["refresh_token"] = "rt-o365"
        });
        _store.SaveOAuthTokens("inst-1", "google", new Dictionary<string, string>
        {
            ["access_token"] = "at-google",
            ["refresh_token"] = "rt-google"
        });

        var o365Tokens = _store.GetOAuthTokens("inst-1", "office365");
        var googleTokens = _store.GetOAuthTokens("inst-1", "google");

        Assert.NotNull(o365Tokens);
        Assert.NotNull(googleTokens);
        Assert.Equal("at-o365", o365Tokens["access_token"]);
        Assert.Equal("at-google", googleTokens["access_token"]);
    }

    #endregion

    #region HasOAuthTokens

    [Fact]
    public void HasOAuthTokens_AfterSave_ReturnsTrue()
    {
        _store.RegisterInstall("inst-1", "tool-1");
        _store.SaveOAuthTokens("inst-1", "google", new Dictionary<string, string>
        {
            ["access_token"] = "at"
        });

        Assert.True(_store.HasOAuthTokens("inst-1", "google"));
    }

    [Fact]
    public void HasOAuthTokens_NoTokens_ReturnsFalse()
    {
        _store.RegisterInstall("inst-1", "tool-1");

        Assert.False(_store.HasOAuthTokens("inst-1", "google"));
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

    #endregion

    #region Bundle Token Sharing

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

    #endregion

    #region RemoveInstall

    [Fact]
    public void RemoveInstall_CleansUpOAuthTokens()
    {
        _store.RegisterInstall("inst-1", "tool-1");
        _store.SaveOAuthTokens("inst-1", "office365", new Dictionary<string, string>
        {
            ["access_token"] = "at",
            ["refresh_token"] = "rt"
        });
        _store.SaveOAuthTokens("inst-1", "google", new Dictionary<string, string>
        {
            ["access_token"] = "at2",
            ["refresh_token"] = "rt2"
        });

        _store.RemoveInstall("inst-1");

        Assert.False(_store.HasOAuthTokens("inst-1", "office365"));
        Assert.False(_store.HasOAuthTokens("inst-1", "google"));
        Assert.Null(_store.GetOAuthTokens("inst-1", "office365"));
    }

    [Fact]
    public void RemoveInstall_DoesNotAffectOtherInstalls()
    {
        _store.RegisterInstall("inst-1", "tool-1");
        _store.RegisterInstall("inst-2", "tool-1");
        _store.SaveOAuthTokens("inst-1", "office365", new Dictionary<string, string>
        {
            ["access_token"] = "at1"
        });
        _store.SaveOAuthTokens("inst-2", "office365", new Dictionary<string, string>
        {
            ["access_token"] = "at2"
        });

        _store.RemoveInstall("inst-1");

        Assert.False(_store.HasOAuthTokens("inst-1", "office365"));
        Assert.True(_store.HasOAuthTokens("inst-2", "office365"));
    }

    [Fact]
    public void RemoveInstall_ClearsBundleMapping()
    {
        _store.RegisterInstall("inst-001", "tool-a", "binst-100");

        _store.RemoveInstall("inst-001");

        Assert.Null(_store.GetBundleInstallId("inst-001"));
        Assert.False(_store.IsInstalled("inst-001"));
    }

    #endregion
}

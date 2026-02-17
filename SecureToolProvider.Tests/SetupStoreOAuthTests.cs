namespace SecureToolProvider.Tests;

public class SetupStoreOAuthTests
{
    private readonly SetupStore _store = new();

    [Fact]
    public void SaveOAuthTokens_StoresTokensSuccessfully()
    {
        _store.RegisterInstall("inst-1", "tool-1");
        var expires = DateTime.UtcNow.AddHours(1);

        _store.SaveOAuthTokens("inst-1", "office365", "access-token", "refresh-token", expires);

        var tokens = _store.GetOAuthTokens("inst-1", "office365");
        Assert.NotNull(tokens);
        Assert.Equal("access-token", tokens.AccessToken);
        Assert.Equal("refresh-token", tokens.RefreshToken);
        Assert.Equal(expires, tokens.ExpiresAt);
    }

    [Fact]
    public void GetOAuthTokens_UnknownService_ReturnsNull()
    {
        _store.RegisterInstall("inst-1", "tool-1");

        var tokens = _store.GetOAuthTokens("inst-1", "nonexistent");

        Assert.Null(tokens);
    }

    [Fact]
    public void IsOAuthConnected_AfterSave_ReturnsTrue()
    {
        _store.RegisterInstall("inst-1", "tool-1");
        _store.SaveOAuthTokens("inst-1", "google", "at", "rt", DateTime.UtcNow.AddHours(1));

        Assert.True(_store.IsOAuthConnected("inst-1", "google"));
    }

    [Fact]
    public void IsOAuthConnected_NoTokens_ReturnsFalse()
    {
        _store.RegisterInstall("inst-1", "tool-1");

        Assert.False(_store.IsOAuthConnected("inst-1", "google"));
    }

    [Fact]
    public void SaveOAuthTokens_OverwritesExisting()
    {
        _store.RegisterInstall("inst-1", "tool-1");
        _store.SaveOAuthTokens("inst-1", "office365", "old-token", "old-refresh", DateTime.UtcNow);

        var newExpiry = DateTime.UtcNow.AddHours(2);
        _store.SaveOAuthTokens("inst-1", "office365", "new-token", "new-refresh", newExpiry);

        var tokens = _store.GetOAuthTokens("inst-1", "office365");
        Assert.NotNull(tokens);
        Assert.Equal("new-token", tokens.AccessToken);
        Assert.Equal("new-refresh", tokens.RefreshToken);
    }

    [Fact]
    public void RemoveInstall_CleansUpOAuthTokens()
    {
        _store.RegisterInstall("inst-1", "tool-1");
        _store.SaveOAuthTokens("inst-1", "office365", "at", "rt", DateTime.UtcNow.AddHours(1));
        _store.SaveOAuthTokens("inst-1", "google", "at2", "rt2", DateTime.UtcNow.AddHours(1));

        _store.RemoveInstall("inst-1");

        Assert.False(_store.IsOAuthConnected("inst-1", "office365"));
        Assert.False(_store.IsOAuthConnected("inst-1", "google"));
        Assert.Null(_store.GetOAuthTokens("inst-1", "office365"));
    }

    [Fact]
    public void RemoveInstall_DoesNotAffectOtherInstalls()
    {
        _store.RegisterInstall("inst-1", "tool-1");
        _store.RegisterInstall("inst-2", "tool-1");
        _store.SaveOAuthTokens("inst-1", "office365", "at1", "rt1", DateTime.UtcNow.AddHours(1));
        _store.SaveOAuthTokens("inst-2", "office365", "at2", "rt2", DateTime.UtcNow.AddHours(1));

        _store.RemoveInstall("inst-1");

        Assert.False(_store.IsOAuthConnected("inst-1", "office365"));
        Assert.True(_store.IsOAuthConnected("inst-2", "office365"));
    }

    [Fact]
    public void MultipleServices_SameInstall_IndependentlyTracked()
    {
        _store.RegisterInstall("inst-1", "tool-1");
        _store.SaveOAuthTokens("inst-1", "office365", "at-o365", "rt-o365", DateTime.UtcNow.AddHours(1));
        _store.SaveOAuthTokens("inst-1", "google", "at-google", "rt-google", DateTime.UtcNow.AddHours(2));

        var o365Tokens = _store.GetOAuthTokens("inst-1", "office365");
        var googleTokens = _store.GetOAuthTokens("inst-1", "google");

        Assert.NotNull(o365Tokens);
        Assert.NotNull(googleTokens);
        Assert.Equal("at-o365", o365Tokens.AccessToken);
        Assert.Equal("at-google", googleTokens.AccessToken);
    }
}

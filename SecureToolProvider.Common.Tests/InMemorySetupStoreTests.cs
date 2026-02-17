using SecureToolProvider.Common;

namespace SecureToolProvider.Common.Tests;

public class InMemorySetupStoreTests
{
    private readonly InMemorySetupStore _store = new();

    [Fact]
    public async Task RegisterInstall_MakesInstallIdKnown()
    {
        await _store.RegisterInstallAsync("inst-1", "tool-1");
        Assert.True(await _store.IsInstalledAsync("inst-1"));
    }

    [Fact]
    public async Task IsInstalled_ReturnsFalseForUnknownId()
    {
        Assert.False(await _store.IsInstalledAsync("unknown"));
    }

    [Fact]
    public async Task RemoveInstall_RemovesInstallationAndSetup()
    {
        await _store.RegisterInstallAsync("inst-1", "tool-1");
        await _store.SaveSetupAsync("inst-1", new Dictionary<string, string> { ["key"] = "val" });

        var removed = await _store.RemoveInstallAsync("inst-1");

        Assert.True(removed);
        Assert.False(await _store.IsInstalledAsync("inst-1"));
        Assert.Null(await _store.GetSetupAsync("inst-1"));
    }

    [Fact]
    public async Task RemoveInstall_ReturnsFalseForUnknownId()
    {
        Assert.False(await _store.RemoveInstallAsync("unknown"));
    }

    [Fact]
    public async Task SaveAndGetSetup_RoundTrips()
    {
        await _store.RegisterInstallAsync("inst-1", "tool-1");
        var values = new Dictionary<string, string> { ["apiKey"] = "secret123", ["baseUrl"] = "https://example.com" };

        await _store.SaveSetupAsync("inst-1", values);
        var retrieved = await _store.GetSetupAsync("inst-1");

        Assert.NotNull(retrieved);
        Assert.Equal("secret123", retrieved["apiKey"]);
        Assert.Equal("https://example.com", retrieved["baseUrl"]);
    }

    [Fact]
    public async Task GetSetup_ReturnsNullWhenNotConfigured()
    {
        await _store.RegisterInstallAsync("inst-1", "tool-1");
        Assert.Null(await _store.GetSetupAsync("inst-1"));
    }

    [Fact]
    public async Task GetToolId_ReturnsToolId()
    {
        await _store.RegisterInstallAsync("inst-1", "tool-abc");
        Assert.Equal("tool-abc", await _store.GetToolIdAsync("inst-1"));
    }

    [Fact]
    public async Task GetToolId_ReturnsNullForUnknownId()
    {
        Assert.Null(await _store.GetToolIdAsync("unknown"));
    }

    [Fact]
    public async Task SaveSetup_OverwritesPreviousValues()
    {
        await _store.RegisterInstallAsync("inst-1", "tool-1");
        await _store.SaveSetupAsync("inst-1", new Dictionary<string, string> { ["key"] = "old" });
        await _store.SaveSetupAsync("inst-1", new Dictionary<string, string> { ["key"] = "new" });

        var setup = await _store.GetSetupAsync("inst-1");
        Assert.Equal("new", setup!["key"]);
    }
}

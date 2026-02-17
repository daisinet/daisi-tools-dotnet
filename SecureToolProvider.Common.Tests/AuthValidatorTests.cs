using Microsoft.Extensions.Configuration;

namespace SecureToolProvider.Common.Tests;

public class AuthValidatorTests
{
    private static AuthValidator CreateValidator(string authKey = "test-secret")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DaisiAuthKey"] = authKey
            })
            .Build();
        return new AuthValidator(config);
    }

    [Fact]
    public async Task VerifyInstallId_ReturnsTrueForKnownInstall()
    {
        var validator = CreateValidator();
        var store = new InMemorySetupStore();
        await store.RegisterInstallAsync("inst-1", "tool-1");

        Assert.True(await validator.VerifyInstallIdAsync("inst-1", store));
    }

    [Fact]
    public async Task VerifyInstallId_ReturnsFalseForUnknownInstall()
    {
        var validator = CreateValidator();
        var store = new InMemorySetupStore();

        Assert.False(await validator.VerifyInstallIdAsync("unknown", store));
    }

    [Fact]
    public async Task VerifyInstallId_ReturnsFalseForNullOrEmpty()
    {
        var validator = CreateValidator();
        var store = new InMemorySetupStore();

        Assert.False(await validator.VerifyInstallIdAsync(null, store));
        Assert.False(await validator.VerifyInstallIdAsync("", store));
    }
}

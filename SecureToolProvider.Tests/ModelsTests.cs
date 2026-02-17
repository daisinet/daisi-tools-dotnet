namespace SecureToolProvider.Tests;

/// <summary>
/// Smoke tests for the OAuth request/response model classes.
/// </summary>
public class ModelsTests
{
    [Fact]
    public void AuthStatusRequest_DefaultValues()
    {
        var req = new AuthStatusRequest();

        Assert.Equal(string.Empty, req.InstallId);
        Assert.Equal(string.Empty, req.Service);
    }

    [Fact]
    public void AuthStatusResponse_DefaultValues()
    {
        var resp = new AuthStatusResponse();

        Assert.False(resp.Connected);
        Assert.Null(resp.ServiceName);
        Assert.Null(resp.UserLabel);
    }

    [Fact]
    public void AuthStatusResponse_SetConnected()
    {
        var resp = new AuthStatusResponse
        {
            Connected = true,
            ServiceName = "office365",
            UserLabel = "user@example.com"
        };

        Assert.True(resp.Connected);
        Assert.Equal("office365", resp.ServiceName);
        Assert.Equal("user@example.com", resp.UserLabel);
    }

    [Fact]
    public void OAuthTokenData_RecordEquality()
    {
        var expires = DateTime.UtcNow.AddHours(1);
        var token1 = new OAuthTokenData("access", "refresh", expires);
        var token2 = new OAuthTokenData("access", "refresh", expires);

        Assert.Equal(token1, token2);
    }

    [Fact]
    public void OAuthTokenData_RecordInequality()
    {
        var expires = DateTime.UtcNow.AddHours(1);
        var token1 = new OAuthTokenData("access-1", "refresh", expires);
        var token2 = new OAuthTokenData("access-2", "refresh", expires);

        Assert.NotEqual(token1, token2);
    }
}

using System.Text.Json;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;
using Daisi.SecureTools.Comms.XDm.Tools;

namespace Daisi.SecureTools.Tests.Comms;

public class XDmSendToolTests
{
    private static SocialHttpClient CreateSocialClient(CommsMockHttpHandler handler)
    {
        var factory = new CommsMockHttpClientFactory(handler);
        return new SocialHttpClient(factory);
    }

    [Fact]
    public async Task XDmSendTool_SendsDirectMessage()
    {
        var handler = new CommsMockHttpHandler(
            JsonSerializer.Serialize(new { data = new { dm_event_id = "dm-123" } }));
        var factory = new CommsMockHttpClientFactory(handler);
        var socialClient = new SocialHttpClient(factory);
        var tool = new XDmSendTool(factory);

        var result = await tool.ExecuteAsync(socialClient, "oauth-access-token",
        [
            new ParameterValue { Name = "recipientId", Value = "user-456" },
            new ParameterValue { Name = "text", Value = "Hello via DM!" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("dm-123", result.Output);
        Assert.Equal("json", result.OutputFormat);
    }

    [Fact]
    public async Task XDmSendTool_RequiresRecipientId()
    {
        var handler = new CommsMockHttpHandler("{}");
        var factory = new CommsMockHttpClientFactory(handler);
        var tool = new XDmSendTool(factory);

        var result = await tool.ExecuteAsync(CreateSocialClient(handler), "token",
            [new ParameterValue { Name = "text", Value = "Hello" }]);

        Assert.False(result.Success);
        Assert.Contains("recipientId", result.ErrorMessage);
    }

    [Fact]
    public async Task XDmSendTool_RequiresText()
    {
        var handler = new CommsMockHttpHandler("{}");
        var factory = new CommsMockHttpClientFactory(handler);
        var tool = new XDmSendTool(factory);

        var result = await tool.ExecuteAsync(CreateSocialClient(handler), "token",
            [new ParameterValue { Name = "recipientId", Value = "user-456" }]);

        Assert.False(result.Success);
        Assert.Contains("text", result.ErrorMessage);
    }
}

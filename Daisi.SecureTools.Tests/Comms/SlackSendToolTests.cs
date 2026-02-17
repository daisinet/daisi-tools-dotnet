using System.Text.Json;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;
using Daisi.SecureTools.Comms.Slack.Tools;

namespace Daisi.SecureTools.Tests.Comms;

public class SlackSendToolTests
{
    private static SocialHttpClient CreateSocialClient(CommsMockHttpHandler handler)
    {
        var factory = new CommsMockHttpClientFactory(handler);
        return new SocialHttpClient(factory);
    }

    [Fact]
    public async Task SlackSendTool_SendsMessage()
    {
        var handler = new CommsMockHttpHandler(
            JsonSerializer.Serialize(new { ok = true, ts = "1234567890.123456", channel = "C01ABC" }));
        var factory = new CommsMockHttpClientFactory(handler);
        var socialClient = new SocialHttpClient(factory);
        var tool = new SlackSendTool(factory);

        var result = await tool.ExecuteAsync(socialClient, "xoxb-slack-token",
        [
            new ParameterValue { Name = "channel", Value = "C01ABC" },
            new ParameterValue { Name = "text", Value = "Hello Slack!" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("1234567890.123456", result.Output);
        Assert.Equal("json", result.OutputFormat);
    }

    [Fact]
    public async Task SlackSendTool_SendsThreadedMessage()
    {
        var handler = new CommsMockHttpHandler(
            JsonSerializer.Serialize(new { ok = true, ts = "1234567890.654321", channel = "C01ABC" }));
        var factory = new CommsMockHttpClientFactory(handler);
        var socialClient = new SocialHttpClient(factory);
        var tool = new SlackSendTool(factory);

        var result = await tool.ExecuteAsync(socialClient, "xoxb-slack-token",
        [
            new ParameterValue { Name = "channel", Value = "C01ABC" },
            new ParameterValue { Name = "text", Value = "Thread reply" },
            new ParameterValue { Name = "threadTs", Value = "1234567890.123456" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("1234567890.123456", result.Output);
    }

    [Fact]
    public async Task SlackSendTool_HandlesApiError()
    {
        var handler = new CommsMockHttpHandler(
            JsonSerializer.Serialize(new { ok = false, error = "channel_not_found" }));
        var factory = new CommsMockHttpClientFactory(handler);
        var socialClient = new SocialHttpClient(factory);
        var tool = new SlackSendTool(factory);

        var result = await tool.ExecuteAsync(socialClient, "xoxb-slack-token",
        [
            new ParameterValue { Name = "channel", Value = "C-INVALID" },
            new ParameterValue { Name = "text", Value = "test" }
        ]);

        Assert.False(result.Success);
        Assert.Contains("channel_not_found", result.ErrorMessage);
    }

    [Fact]
    public async Task SlackSendTool_RequiresChannel()
    {
        var handler = new CommsMockHttpHandler("{}");
        var factory = new CommsMockHttpClientFactory(handler);
        var tool = new SlackSendTool(factory);

        var result = await tool.ExecuteAsync(CreateSocialClient(handler), "token",
            [new ParameterValue { Name = "text", Value = "test" }]);

        Assert.False(result.Success);
        Assert.Contains("channel", result.ErrorMessage);
    }

    [Fact]
    public async Task SlackSendTool_RequiresText()
    {
        var handler = new CommsMockHttpHandler("{}");
        var factory = new CommsMockHttpClientFactory(handler);
        var tool = new SlackSendTool(factory);

        var result = await tool.ExecuteAsync(CreateSocialClient(handler), "token",
            [new ParameterValue { Name = "channel", Value = "C01ABC" }]);

        Assert.False(result.Success);
        Assert.Contains("text", result.ErrorMessage);
    }
}

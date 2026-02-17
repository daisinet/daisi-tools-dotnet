using System.Text.Json;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;
using Daisi.SecureTools.Comms.Teams.Tools;

namespace Daisi.SecureTools.Tests.Comms;

public class TeamsSendToolTests
{
    private static SocialHttpClient CreateSocialClient(CommsMockHttpHandler handler)
    {
        var factory = new CommsMockHttpClientFactory(handler);
        return new SocialHttpClient(factory);
    }

    [Fact]
    public async Task TeamsSendTool_SendsTextMessage()
    {
        var handler = new CommsMockHttpHandler(
            JsonSerializer.Serialize(new { id = "msg-001" }));
        var socialClient = CreateSocialClient(handler);
        var tool = new TeamsSendTool();

        var result = await tool.ExecuteAsync(socialClient, "teams-access-token",
        [
            new ParameterValue { Name = "chatId", Value = "19:abc123@thread.v2" },
            new ParameterValue { Name = "message", Value = "Hello Teams!" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("msg-001", result.Output);
        Assert.Equal("json", result.OutputFormat);
    }

    [Fact]
    public async Task TeamsSendTool_SendsHtmlMessage()
    {
        var handler = new CommsMockHttpHandler(
            JsonSerializer.Serialize(new { id = "msg-002" }));
        var socialClient = CreateSocialClient(handler);
        var tool = new TeamsSendTool();

        var result = await tool.ExecuteAsync(socialClient, "teams-access-token",
        [
            new ParameterValue { Name = "chatId", Value = "19:abc123@thread.v2" },
            new ParameterValue { Name = "message", Value = "<b>Bold message</b>" },
            new ParameterValue { Name = "contentType", Value = "html" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("html", result.Output);
    }

    [Fact]
    public async Task TeamsSendTool_RequiresChatId()
    {
        var handler = new CommsMockHttpHandler("{}");
        var tool = new TeamsSendTool();
        var result = await tool.ExecuteAsync(CreateSocialClient(handler), "token",
            [new ParameterValue { Name = "message", Value = "test" }]);

        Assert.False(result.Success);
        Assert.Contains("chatId", result.ErrorMessage);
    }

    [Fact]
    public async Task TeamsSendTool_RequiresMessage()
    {
        var handler = new CommsMockHttpHandler("{}");
        var tool = new TeamsSendTool();
        var result = await tool.ExecuteAsync(CreateSocialClient(handler), "token",
            [new ParameterValue { Name = "chatId", Value = "19:abc@thread.v2" }]);

        Assert.False(result.Success);
        Assert.Contains("message", result.ErrorMessage);
    }
}

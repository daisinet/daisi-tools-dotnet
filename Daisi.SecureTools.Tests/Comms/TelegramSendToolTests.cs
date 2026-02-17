using System.Text.Json;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;
using Daisi.SecureTools.Comms.Telegram.Tools;

namespace Daisi.SecureTools.Tests.Comms;

public class TelegramSendToolTests
{
    private static (SocialHttpClient, CommsMockHttpHandler) CreateSocialClient(
        params (string Content, System.Net.HttpStatusCode StatusCode)[] responses)
    {
        var handler = new CommsMockHttpHandler(responses);
        var factory = new CommsMockHttpClientFactory(handler);
        return (new SocialHttpClient(factory), handler);
    }

    [Fact]
    public async Task TelegramSendTool_SendsTextMessage()
    {
        var (socialClient, handler) = CreateSocialClient(
            (JsonSerializer.Serialize(new { ok = true, result = new { message_id = 42 } }), System.Net.HttpStatusCode.OK));
        var factory = new CommsMockHttpClientFactory(handler);
        var tool = new TelegramSendTool(factory);

        var result = await tool.ExecuteAsync(socialClient, "bot-token-123",
        [
            new ParameterValue { Name = "chatId", Value = "12345" },
            new ParameterValue { Name = "text", Value = "Hello Telegram!" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("42", result.Output);
        Assert.Contains("text", result.Output);
        Assert.Equal("json", result.OutputFormat);
    }

    [Fact]
    public async Task TelegramSendTool_SendsTextWithParseMode()
    {
        var (socialClient, handler) = CreateSocialClient(
            (JsonSerializer.Serialize(new { ok = true, result = new { message_id = 43 } }), System.Net.HttpStatusCode.OK));
        var factory = new CommsMockHttpClientFactory(handler);
        var tool = new TelegramSendTool(factory);

        var result = await tool.ExecuteAsync(socialClient, "bot-token-123",
        [
            new ParameterValue { Name = "chatId", Value = "12345" },
            new ParameterValue { Name = "text", Value = "*bold* text" },
            new ParameterValue { Name = "parseMode", Value = "MarkdownV2" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("43", result.Output);
    }

    [Fact]
    public async Task TelegramSendTool_RequiresChatId()
    {
        var handler = new CommsMockHttpHandler("{}");
        var factory = new CommsMockHttpClientFactory(handler);
        var tool = new TelegramSendTool(factory);
        var socialClient = new SocialHttpClient(factory);

        var result = await tool.ExecuteAsync(socialClient, "bot-token",
            [new ParameterValue { Name = "text", Value = "test" }]);

        Assert.False(result.Success);
        Assert.Contains("chatId", result.ErrorMessage);
    }

    [Fact]
    public async Task TelegramSendTool_RequiresTextOrMedia()
    {
        var handler = new CommsMockHttpHandler("{}");
        var factory = new CommsMockHttpClientFactory(handler);
        var tool = new TelegramSendTool(factory);
        var socialClient = new SocialHttpClient(factory);

        var result = await tool.ExecuteAsync(socialClient, "bot-token",
            [new ParameterValue { Name = "chatId", Value = "12345" }]);

        Assert.False(result.Success);
        Assert.Contains("text", result.ErrorMessage);
    }
}

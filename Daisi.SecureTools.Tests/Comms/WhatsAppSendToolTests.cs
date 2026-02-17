using System.Text.Json;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;
using Daisi.SecureTools.Comms.WhatsApp.Tools;

namespace Daisi.SecureTools.Tests.Comms;

public class WhatsAppSendToolTests
{
    private static SocialHttpClient CreateSocialClient(CommsMockHttpHandler handler)
    {
        var factory = new CommsMockHttpClientFactory(handler);
        return new SocialHttpClient(factory);
    }

    [Fact]
    public async Task WhatsAppSendTool_SendsTextMessage()
    {
        var handler = new CommsMockHttpHandler(
            JsonSerializer.Serialize(new { messages = new[] { new { id = "wamid.123" } } }));
        var socialClient = CreateSocialClient(handler);
        var tool = new WhatsAppSendTool();

        var result = await tool.ExecuteAsync(socialClient, "access-token",
        [
            new ParameterValue { Name = "phoneNumberId", Value = "123456789" },
            new ParameterValue { Name = "to", Value = "+15551234567" },
            new ParameterValue { Name = "text", Value = "Hello from WhatsApp!" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("wamid.123", result.Output);
        Assert.Contains("text", result.Output);
        Assert.Equal("json", result.OutputFormat);
    }

    [Fact]
    public async Task WhatsAppSendTool_SendsTemplateMessage()
    {
        var handler = new CommsMockHttpHandler(
            JsonSerializer.Serialize(new { messages = new[] { new { id = "wamid.456" } } }));
        var socialClient = CreateSocialClient(handler);
        var tool = new WhatsAppSendTool();

        var result = await tool.ExecuteAsync(socialClient, "access-token",
        [
            new ParameterValue { Name = "phoneNumberId", Value = "123456789" },
            new ParameterValue { Name = "to", Value = "+15551234567" },
            new ParameterValue { Name = "templateName", Value = "hello_world" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("wamid.456", result.Output);
        Assert.Contains("template", result.Output);
    }

    [Fact]
    public async Task WhatsAppSendTool_SendsMediaMessage()
    {
        var handler = new CommsMockHttpHandler(
            JsonSerializer.Serialize(new { messages = new[] { new { id = "wamid.789" } } }));
        var socialClient = CreateSocialClient(handler);
        var tool = new WhatsAppSendTool();

        var result = await tool.ExecuteAsync(socialClient, "access-token",
        [
            new ParameterValue { Name = "phoneNumberId", Value = "123456789" },
            new ParameterValue { Name = "to", Value = "+15551234567" },
            new ParameterValue { Name = "mediaUrl", Value = "https://example.com/image.jpg" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("wamid.789", result.Output);
        Assert.Contains("image", result.Output);
    }

    [Fact]
    public async Task WhatsAppSendTool_RequiresPhoneNumberId()
    {
        var handler = new CommsMockHttpHandler("{}");
        var tool = new WhatsAppSendTool();
        var result = await tool.ExecuteAsync(CreateSocialClient(handler), "token",
            [new ParameterValue { Name = "to", Value = "+15551234567" }]);

        Assert.False(result.Success);
        Assert.Contains("phoneNumberId", result.ErrorMessage);
    }

    [Fact]
    public async Task WhatsAppSendTool_RequiresTo()
    {
        var handler = new CommsMockHttpHandler("{}");
        var tool = new WhatsAppSendTool();
        var result = await tool.ExecuteAsync(CreateSocialClient(handler), "token",
            [new ParameterValue { Name = "phoneNumberId", Value = "123" }]);

        Assert.False(result.Success);
        Assert.Contains("to", result.ErrorMessage);
    }

    [Fact]
    public async Task WhatsAppSendTool_RequiresContent()
    {
        var handler = new CommsMockHttpHandler("{}");
        var tool = new WhatsAppSendTool();
        var result = await tool.ExecuteAsync(CreateSocialClient(handler), "token",
        [
            new ParameterValue { Name = "phoneNumberId", Value = "123" },
            new ParameterValue { Name = "to", Value = "+15551234567" }
        ]);

        Assert.False(result.Success);
        Assert.Contains("text", result.ErrorMessage);
    }
}

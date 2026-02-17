using System.Text.Json;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;
using Daisi.SecureTools.Comms.Twilio.Tools;

namespace Daisi.SecureTools.Tests.Comms;

public class TwilioToolTests
{
    private static readonly string BasicAuth = Convert.ToBase64String(
        System.Text.Encoding.UTF8.GetBytes("AC1234567890:auth-token-secret"));

    private static SocialHttpClient CreateSocialClient(CommsMockHttpHandler handler)
    {
        var factory = new CommsMockHttpClientFactory(handler);
        return new SocialHttpClient(factory);
    }

    // --- SMS Tool Tests ---

    [Fact]
    public async Task TwilioSmsTool_SendsSms()
    {
        var handler = new CommsMockHttpHandler(
            JsonSerializer.Serialize(new { sid = "SM123", status = "queued" }));
        var socialClient = CreateSocialClient(handler);
        var tool = new TwilioSmsTool();

        var result = await tool.ExecuteAsync(socialClient, BasicAuth,
        [
            new ParameterValue { Name = "to", Value = "+15551234567" },
            new ParameterValue { Name = "body", Value = "Hello from Daisi!" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("SM123", result.Output);
        Assert.Equal("json", result.OutputFormat);
    }

    [Fact]
    public async Task TwilioSmsTool_RequiresTo()
    {
        var handler = new CommsMockHttpHandler("{}");
        var tool = new TwilioSmsTool();
        var result = await tool.ExecuteAsync(CreateSocialClient(handler), BasicAuth,
            [new ParameterValue { Name = "body", Value = "test" }]);

        Assert.False(result.Success);
        Assert.Contains("to", result.ErrorMessage);
    }

    [Fact]
    public async Task TwilioSmsTool_RequiresBody()
    {
        var handler = new CommsMockHttpHandler("{}");
        var tool = new TwilioSmsTool();
        var result = await tool.ExecuteAsync(CreateSocialClient(handler), BasicAuth,
            [new ParameterValue { Name = "to", Value = "+15551234567" }]);

        Assert.False(result.Success);
        Assert.Contains("body", result.ErrorMessage);
    }

    // --- Voice Tool Tests ---

    [Fact]
    public async Task TwilioVoiceTool_InitiatesCall()
    {
        var handler = new CommsMockHttpHandler(
            JsonSerializer.Serialize(new { sid = "CA456", status = "queued" }));
        var socialClient = CreateSocialClient(handler);
        var tool = new TwilioVoiceTool();

        var result = await tool.ExecuteAsync(socialClient, BasicAuth,
        [
            new ParameterValue { Name = "to", Value = "+15551234567" },
            new ParameterValue { Name = "twimlUrl", Value = "https://example.com/twiml" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("CA456", result.Output);
        Assert.Equal("json", result.OutputFormat);
    }

    [Fact]
    public async Task TwilioVoiceTool_RequiresTo()
    {
        var handler = new CommsMockHttpHandler("{}");
        var tool = new TwilioVoiceTool();
        var result = await tool.ExecuteAsync(CreateSocialClient(handler), BasicAuth,
            [new ParameterValue { Name = "twimlUrl", Value = "https://example.com/twiml" }]);

        Assert.False(result.Success);
        Assert.Contains("to", result.ErrorMessage);
    }

    [Fact]
    public async Task TwilioVoiceTool_RequiresTwimlUrl()
    {
        var handler = new CommsMockHttpHandler("{}");
        var tool = new TwilioVoiceTool();
        var result = await tool.ExecuteAsync(CreateSocialClient(handler), BasicAuth,
            [new ParameterValue { Name = "to", Value = "+15551234567" }]);

        Assert.False(result.Success);
        Assert.Contains("twimlUrl", result.ErrorMessage);
    }

    // --- Email Tool Tests ---

    [Fact]
    public async Task TwilioEmailTool_SendsEmail()
    {
        var handler = new CommsMockHttpHandler("{}", System.Net.HttpStatusCode.Accepted);
        var socialClient = CreateSocialClient(handler);
        var tool = new TwilioEmailTool();

        var result = await tool.ExecuteAsync(socialClient, "SG.sendgrid-api-key",
        [
            new ParameterValue { Name = "to", Value = "user@example.com" },
            new ParameterValue { Name = "subject", Value = "Test Subject" },
            new ParameterValue { Name = "body", Value = "Hello from Daisi!" },
            new ParameterValue { Name = "from", Value = "sender@example.com" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("user@example.com", result.Output);
        Assert.Equal("json", result.OutputFormat);
    }

    [Fact]
    public async Task TwilioEmailTool_RequiresTo()
    {
        var handler = new CommsMockHttpHandler("{}");
        var tool = new TwilioEmailTool();
        var result = await tool.ExecuteAsync(CreateSocialClient(handler), "api-key",
        [
            new ParameterValue { Name = "subject", Value = "Test" },
            new ParameterValue { Name = "body", Value = "Body" }
        ]);

        Assert.False(result.Success);
        Assert.Contains("to", result.ErrorMessage);
    }

    [Fact]
    public async Task TwilioEmailTool_RequiresSubject()
    {
        var handler = new CommsMockHttpHandler("{}");
        var tool = new TwilioEmailTool();
        var result = await tool.ExecuteAsync(CreateSocialClient(handler), "api-key",
        [
            new ParameterValue { Name = "to", Value = "user@example.com" },
            new ParameterValue { Name = "body", Value = "Body" }
        ]);

        Assert.False(result.Success);
        Assert.Contains("subject", result.ErrorMessage);
    }

    [Fact]
    public async Task TwilioEmailTool_RequiresBody()
    {
        var handler = new CommsMockHttpHandler("{}");
        var tool = new TwilioEmailTool();
        var result = await tool.ExecuteAsync(CreateSocialClient(handler), "api-key",
        [
            new ParameterValue { Name = "to", Value = "user@example.com" },
            new ParameterValue { Name = "subject", Value = "Test" }
        ]);

        Assert.False(result.Success);
        Assert.Contains("body", result.ErrorMessage);
    }

    [Fact]
    public async Task TwilioEmailTool_SupportsHtmlContent()
    {
        var handler = new CommsMockHttpHandler("{}", System.Net.HttpStatusCode.Accepted);
        var socialClient = CreateSocialClient(handler);
        var tool = new TwilioEmailTool();

        var result = await tool.ExecuteAsync(socialClient, "SG.api-key",
        [
            new ParameterValue { Name = "to", Value = "user@example.com" },
            new ParameterValue { Name = "subject", Value = "HTML Test" },
            new ParameterValue { Name = "body", Value = "<h1>Hello</h1>" },
            new ParameterValue { Name = "isHtml", Value = "true" }
        ]);

        Assert.True(result.Success);
        Assert.Contains("text/html", result.Output);
    }
}

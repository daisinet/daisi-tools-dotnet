using System.Text;
using System.Text.Json;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Google;
using Daisi.SecureTools.Google.Tools;

namespace Daisi.SecureTools.Tests.Google;

public class GoogleToolTests
{
    // =========================================================================
    // GoogleServiceFactory Tests
    // =========================================================================

    [Fact]
    public void GoogleServiceFactory_CreatesGmailService()
    {
        var factory = new GoogleServiceFactory();
        var service = factory.CreateGmailService("test-token");
        Assert.NotNull(service);
        Assert.Equal("Daisi SecureTools Google", service.ApplicationName);
    }

    [Fact]
    public void GoogleServiceFactory_CreatesDriveService()
    {
        var factory = new GoogleServiceFactory();
        var service = factory.CreateDriveService("test-token");
        Assert.NotNull(service);
        Assert.Equal("Daisi SecureTools Google", service.ApplicationName);
    }

    [Fact]
    public void GoogleServiceFactory_CreatesCalendarService()
    {
        var factory = new GoogleServiceFactory();
        var service = factory.CreateCalendarService("test-token");
        Assert.NotNull(service);
        Assert.Equal("Daisi SecureTools Google", service.ApplicationName);
    }

    [Fact]
    public void GoogleServiceFactory_CreatesSheetsService()
    {
        var factory = new GoogleServiceFactory();
        var service = factory.CreateSheetsService("test-token");
        Assert.NotNull(service);
        Assert.Equal("Daisi SecureTools Google", service.ApplicationName);
    }

    // =========================================================================
    // GmailSearchTool — parameter validation
    // =========================================================================

    [Fact]
    public async Task GmailSearchTool_RequiresQuery()
    {
        var tool = new GmailSearchTool();
        var result = await tool.ExecuteAsync(new GoogleServiceFactory(), "token", []);
        Assert.False(result.Success);
        Assert.Contains("query", result.ErrorMessage);
    }

    // =========================================================================
    // GmailReadTool — parameter validation and utility methods
    // =========================================================================

    [Fact]
    public async Task GmailReadTool_RequiresMessageId()
    {
        var tool = new GmailReadTool();
        var result = await tool.ExecuteAsync(new GoogleServiceFactory(), "token", []);
        Assert.False(result.Success);
        Assert.Contains("messageId", result.ErrorMessage);
    }

    [Fact]
    public void GmailReadTool_DecodeBase64Url_DecodesCorrectly()
    {
        // "Hello, World!" in base64url
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("Hello, World!"))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        var result = GmailReadTool.DecodeBase64Url(encoded);
        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public void GmailReadTool_DecodeBase64Url_HandlesSpecialCharacters()
    {
        var original = "Test with special chars: +/= and unicode \u00e9";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(original))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        var result = GmailReadTool.DecodeBase64Url(encoded);
        Assert.Equal(original, result);
    }

    [Fact]
    public void GmailReadTool_ExtractBody_ReturnsEmptyForNullPayload()
    {
        var result = GmailReadTool.ExtractBody(null);
        Assert.Equal("", result);
    }

    // =========================================================================
    // GmailSendTool — parameter validation and utility methods
    // =========================================================================

    [Fact]
    public async Task GmailSendTool_RequiresTo()
    {
        var tool = new GmailSendTool();
        var result = await tool.ExecuteAsync(new GoogleServiceFactory(), "token", []);
        Assert.False(result.Success);
        Assert.Contains("to", result.ErrorMessage);
    }

    [Fact]
    public async Task GmailSendTool_RequiresSubject()
    {
        var tool = new GmailSendTool();
        var result = await tool.ExecuteAsync(new GoogleServiceFactory(), "token",
            [new ParameterValue { Name = "to", Value = "test@example.com" }]);
        Assert.False(result.Success);
        Assert.Contains("subject", result.ErrorMessage);
    }

    [Fact]
    public async Task GmailSendTool_RequiresBody()
    {
        var tool = new GmailSendTool();
        var result = await tool.ExecuteAsync(new GoogleServiceFactory(), "token",
        [
            new ParameterValue { Name = "to", Value = "test@example.com" },
            new ParameterValue { Name = "subject", Value = "Test" }
        ]);
        Assert.False(result.Success);
        Assert.Contains("body", result.ErrorMessage);
    }

    [Fact]
    public void GmailSendTool_BuildMimeMessage_BasicMessage()
    {
        var mime = GmailSendTool.BuildMimeMessage("user@example.com", "Test Subject", "Hello body", null, null);

        Assert.Contains("To: user@example.com", mime);
        Assert.Contains("Subject: Test Subject", mime);
        Assert.Contains("Content-Type: text/plain; charset=utf-8", mime);
        Assert.Contains("Hello body", mime);
        Assert.DoesNotContain("Cc:", mime);
        Assert.DoesNotContain("Bcc:", mime);
    }

    [Fact]
    public void GmailSendTool_BuildMimeMessage_WithCcAndBcc()
    {
        var mime = GmailSendTool.BuildMimeMessage(
            "to@example.com", "Subject", "Body",
            "cc@example.com", "bcc@example.com");

        Assert.Contains("To: to@example.com", mime);
        Assert.Contains("Cc: cc@example.com", mime);
        Assert.Contains("Bcc: bcc@example.com", mime);
    }

    [Fact]
    public void GmailSendTool_Base64UrlEncode_EncodesCorrectly()
    {
        var input = "Hello, World!";
        var result = GmailSendTool.Base64UrlEncode(input);

        // Should not contain standard base64 padding or special chars
        Assert.DoesNotContain("+", result);
        Assert.DoesNotContain("/", result);
        Assert.DoesNotContain("=", result);

        // Should decode back
        var decoded = result.Replace('-', '+').Replace('_', '/');
        switch (decoded.Length % 4)
        {
            case 2: decoded += "=="; break;
            case 3: decoded += "="; break;
        }
        var bytes = Convert.FromBase64String(decoded);
        Assert.Equal(input, Encoding.UTF8.GetString(bytes));
    }

    // =========================================================================
    // DriveSearchTool — parameter validation
    // =========================================================================

    [Fact]
    public async Task DriveSearchTool_RequiresQuery()
    {
        var tool = new DriveSearchTool();
        var result = await tool.ExecuteAsync(new GoogleServiceFactory(), "token", []);
        Assert.False(result.Success);
        Assert.Contains("query", result.ErrorMessage);
    }

    // =========================================================================
    // DriveReadTool — parameter validation
    // =========================================================================

    [Fact]
    public async Task DriveReadTool_RequiresFileId()
    {
        var tool = new DriveReadTool();
        var result = await tool.ExecuteAsync(new GoogleServiceFactory(), "token", []);
        Assert.False(result.Success);
        Assert.Contains("fileId", result.ErrorMessage);
    }

    // =========================================================================
    // CalendarCreateTool — parameter validation
    // =========================================================================

    [Fact]
    public async Task CalendarCreateTool_RequiresSummary()
    {
        var tool = new CalendarCreateTool();
        var result = await tool.ExecuteAsync(new GoogleServiceFactory(), "token", []);
        Assert.False(result.Success);
        Assert.Contains("summary", result.ErrorMessage);
    }

    [Fact]
    public async Task CalendarCreateTool_RequiresStart()
    {
        var tool = new CalendarCreateTool();
        var result = await tool.ExecuteAsync(new GoogleServiceFactory(), "token",
            [new ParameterValue { Name = "summary", Value = "Meeting" }]);
        Assert.False(result.Success);
        Assert.Contains("start", result.ErrorMessage);
    }

    [Fact]
    public async Task CalendarCreateTool_RequiresEnd()
    {
        var tool = new CalendarCreateTool();
        var result = await tool.ExecuteAsync(new GoogleServiceFactory(), "token",
        [
            new ParameterValue { Name = "summary", Value = "Meeting" },
            new ParameterValue { Name = "start", Value = "2026-02-16T09:00:00Z" }
        ]);
        Assert.False(result.Success);
        Assert.Contains("end", result.ErrorMessage);
    }

    [Fact]
    public async Task CalendarCreateTool_ValidatesStartDateFormat()
    {
        var tool = new CalendarCreateTool();
        var result = await tool.ExecuteAsync(new GoogleServiceFactory(), "token",
        [
            new ParameterValue { Name = "summary", Value = "Meeting" },
            new ParameterValue { Name = "start", Value = "not-a-date" },
            new ParameterValue { Name = "end", Value = "2026-02-16T10:00:00Z" }
        ]);
        Assert.False(result.Success);
        Assert.Contains("start", result.ErrorMessage);
    }

    [Fact]
    public async Task CalendarCreateTool_ValidatesEndDateFormat()
    {
        var tool = new CalendarCreateTool();
        var result = await tool.ExecuteAsync(new GoogleServiceFactory(), "token",
        [
            new ParameterValue { Name = "summary", Value = "Meeting" },
            new ParameterValue { Name = "start", Value = "2026-02-16T09:00:00Z" },
            new ParameterValue { Name = "end", Value = "not-a-date" }
        ]);
        Assert.False(result.Success);
        Assert.Contains("end", result.ErrorMessage);
    }

    // =========================================================================
    // SheetsReadTool — parameter validation
    // =========================================================================

    [Fact]
    public async Task SheetsReadTool_RequiresSpreadsheetId()
    {
        var tool = new SheetsReadTool();
        var result = await tool.ExecuteAsync(new GoogleServiceFactory(), "token", []);
        Assert.False(result.Success);
        Assert.Contains("spreadsheetId", result.ErrorMessage);
    }

    [Fact]
    public async Task SheetsReadTool_RequiresRange()
    {
        var tool = new SheetsReadTool();
        var result = await tool.ExecuteAsync(new GoogleServiceFactory(), "token",
            [new ParameterValue { Name = "spreadsheetId", Value = "abc123" }]);
        Assert.False(result.Success);
        Assert.Contains("range", result.ErrorMessage);
    }

    // =========================================================================
    // SheetsWriteTool — parameter validation
    // =========================================================================

    [Fact]
    public async Task SheetsWriteTool_RequiresSpreadsheetId()
    {
        var tool = new SheetsWriteTool();
        var result = await tool.ExecuteAsync(new GoogleServiceFactory(), "token", []);
        Assert.False(result.Success);
        Assert.Contains("spreadsheetId", result.ErrorMessage);
    }

    [Fact]
    public async Task SheetsWriteTool_RequiresRange()
    {
        var tool = new SheetsWriteTool();
        var result = await tool.ExecuteAsync(new GoogleServiceFactory(), "token",
            [new ParameterValue { Name = "spreadsheetId", Value = "abc123" }]);
        Assert.False(result.Success);
        Assert.Contains("range", result.ErrorMessage);
    }

    [Fact]
    public async Task SheetsWriteTool_RequiresValues()
    {
        var tool = new SheetsWriteTool();
        var result = await tool.ExecuteAsync(new GoogleServiceFactory(), "token",
        [
            new ParameterValue { Name = "spreadsheetId", Value = "abc123" },
            new ParameterValue { Name = "range", Value = "Sheet1!A1:B2" }
        ]);
        Assert.False(result.Success);
        Assert.Contains("values", result.ErrorMessage);
    }

    [Fact]
    public async Task SheetsWriteTool_RejectsInvalidJson()
    {
        var tool = new SheetsWriteTool();
        var result = await tool.ExecuteAsync(new GoogleServiceFactory(), "token",
        [
            new ParameterValue { Name = "spreadsheetId", Value = "abc123" },
            new ParameterValue { Name = "range", Value = "Sheet1!A1:B2" },
            new ParameterValue { Name = "values", Value = "not-json" }
        ]);
        Assert.False(result.Success);
        Assert.Contains("values", result.ErrorMessage);
    }

    [Fact]
    public async Task SheetsWriteTool_RejectsEmptyArray()
    {
        var tool = new SheetsWriteTool();
        var result = await tool.ExecuteAsync(new GoogleServiceFactory(), "token",
        [
            new ParameterValue { Name = "spreadsheetId", Value = "abc123" },
            new ParameterValue { Name = "range", Value = "Sheet1!A1:B2" },
            new ParameterValue { Name = "values", Value = "[]" }
        ]);
        Assert.False(result.Success);
        Assert.Contains("values", result.ErrorMessage);
    }
}

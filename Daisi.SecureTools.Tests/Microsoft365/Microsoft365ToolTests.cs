using Microsoft.Graph;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Microsoft365.Tools;
using GraphClientFactory = Daisi.SecureTools.Microsoft365.GraphClientFactory;

namespace Daisi.SecureTools.Tests.Microsoft365;

public class Microsoft365ToolTests
{
    /// <summary>
    /// Create a GraphServiceClient with a no-op authentication provider for testing.
    /// Calls will fail at the HTTP level but parameter validation runs before any HTTP call.
    /// </summary>
    private static GraphServiceClient CreateTestClient()
    {
        var authProvider = new AnonymousAuthenticationProvider();
        return new GraphServiceClient(authProvider);
    }

    // --- GraphClientFactory ---

    [Fact]
    public void GraphClientFactory_CreatesClient()
    {
        var factory = new GraphClientFactory();
        var client = factory.CreateClient("test-access-token");

        Assert.NotNull(client);
    }

    [Fact]
    public void GraphClientFactory_CreatesDistinctClients()
    {
        var factory = new GraphClientFactory();
        var client1 = factory.CreateClient("token-1");
        var client2 = factory.CreateClient("token-2");

        Assert.NotNull(client1);
        Assert.NotNull(client2);
        Assert.NotSame(client1, client2);
    }

    // --- MailSearchTool ---

    [Fact]
    public async Task MailSearchTool_RequiresQuery()
    {
        var tool = new MailSearchTool();
        var result = await tool.ExecuteAsync(CreateTestClient(), []);

        Assert.False(result.Success);
        Assert.Contains("query", result.ErrorMessage);
    }

    // --- MailUnreadTool ---
    // MailUnreadTool has no required parameters, so we just verify it can be instantiated.

    [Fact]
    public void MailUnreadTool_CanBeInstantiated()
    {
        var tool = new MailUnreadTool();
        Assert.NotNull(tool);
    }

    // --- MailReadTool ---

    [Fact]
    public async Task MailReadTool_RequiresMessageId()
    {
        var tool = new MailReadTool();
        var result = await tool.ExecuteAsync(CreateTestClient(), []);

        Assert.False(result.Success);
        Assert.Contains("messageId", result.ErrorMessage);
    }

    // --- MailSendTool ---

    [Fact]
    public async Task MailSendTool_RequiresTo()
    {
        var tool = new MailSendTool();
        var result = await tool.ExecuteAsync(CreateTestClient(), []);

        Assert.False(result.Success);
        Assert.Contains("to", result.ErrorMessage);
    }

    [Fact]
    public async Task MailSendTool_RequiresSubject()
    {
        var tool = new MailSendTool();
        var result = await tool.ExecuteAsync(CreateTestClient(),
            [new ParameterValue { Name = "to", Value = "test@example.com" }]);

        Assert.False(result.Success);
        Assert.Contains("subject", result.ErrorMessage);
    }

    [Fact]
    public async Task MailSendTool_RequiresBody()
    {
        var tool = new MailSendTool();
        var result = await tool.ExecuteAsync(CreateTestClient(),
        [
            new ParameterValue { Name = "to", Value = "test@example.com" },
            new ParameterValue { Name = "subject", Value = "Test Subject" }
        ]);

        Assert.False(result.Success);
        Assert.Contains("body", result.ErrorMessage);
    }

    // --- OneDriveSearchTool ---

    [Fact]
    public async Task OneDriveSearchTool_RequiresQuery()
    {
        var tool = new OneDriveSearchTool();
        var result = await tool.ExecuteAsync(CreateTestClient(), []);

        Assert.False(result.Success);
        Assert.Contains("query", result.ErrorMessage);
    }

    // --- OneDriveReadTool ---

    [Fact]
    public async Task OneDriveReadTool_RequiresItemId()
    {
        var tool = new OneDriveReadTool();
        var result = await tool.ExecuteAsync(CreateTestClient(), []);

        Assert.False(result.Success);
        Assert.Contains("itemId", result.ErrorMessage);
    }

    // --- CalendarCreateTool ---

    [Fact]
    public async Task CalendarCreateTool_RequiresSubject()
    {
        var tool = new CalendarCreateTool();
        var result = await tool.ExecuteAsync(CreateTestClient(), []);

        Assert.False(result.Success);
        Assert.Contains("subject", result.ErrorMessage);
    }

    [Fact]
    public async Task CalendarCreateTool_RequiresStart()
    {
        var tool = new CalendarCreateTool();
        var result = await tool.ExecuteAsync(CreateTestClient(),
            [new ParameterValue { Name = "subject", Value = "Meeting" }]);

        Assert.False(result.Success);
        Assert.Contains("start", result.ErrorMessage);
    }

    [Fact]
    public async Task CalendarCreateTool_RequiresEnd()
    {
        var tool = new CalendarCreateTool();
        var result = await tool.ExecuteAsync(CreateTestClient(),
        [
            new ParameterValue { Name = "subject", Value = "Meeting" },
            new ParameterValue { Name = "start", Value = "2026-02-16T10:00:00" }
        ]);

        Assert.False(result.Success);
        Assert.Contains("end", result.ErrorMessage);
    }

    // --- TeamsSendTool ---

    [Fact]
    public async Task TeamsSendTool_RequiresTeamId()
    {
        var tool = new TeamsSendTool();
        var result = await tool.ExecuteAsync(CreateTestClient(), []);

        Assert.False(result.Success);
        Assert.Contains("teamId", result.ErrorMessage);
    }

    [Fact]
    public async Task TeamsSendTool_RequiresChannelId()
    {
        var tool = new TeamsSendTool();
        var result = await tool.ExecuteAsync(CreateTestClient(),
            [new ParameterValue { Name = "teamId", Value = "team-123" }]);

        Assert.False(result.Success);
        Assert.Contains("channelId", result.ErrorMessage);
    }

    [Fact]
    public async Task TeamsSendTool_RequiresContent()
    {
        var tool = new TeamsSendTool();
        var result = await tool.ExecuteAsync(CreateTestClient(),
        [
            new ParameterValue { Name = "teamId", Value = "team-123" },
            new ParameterValue { Name = "channelId", Value = "channel-456" }
        ]);

        Assert.False(result.Success);
        Assert.Contains("content", result.ErrorMessage);
    }

    // --- CalendarListTool ---

    [Fact]
    public void CalendarListTool_CanBeInstantiated()
    {
        var tool = new CalendarListTool();
        Assert.NotNull(tool);
    }

    // --- Tool instantiation ---

    [Fact]
    public void AllTools_CanBeInstantiated()
    {
        Assert.NotNull(new MailSearchTool());
        Assert.NotNull(new MailUnreadTool());
        Assert.NotNull(new MailReadTool());
        Assert.NotNull(new MailSendTool());
        Assert.NotNull(new OneDriveSearchTool());
        Assert.NotNull(new OneDriveReadTool());
        Assert.NotNull(new CalendarListTool());
        Assert.NotNull(new CalendarCreateTool());
        Assert.NotNull(new TeamsSendTool());
    }
}

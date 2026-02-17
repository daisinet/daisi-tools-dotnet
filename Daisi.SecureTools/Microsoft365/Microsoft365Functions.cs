using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureToolProvider.Common;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Microsoft365.Tools;

namespace Daisi.SecureTools.Microsoft365;

/// <summary>
/// Azure Functions endpoints for the Microsoft 365 secure tool provider.
/// Supports Outlook mail, OneDrive files, Calendar events, and Teams messaging
/// via the Microsoft Graph API with delegated OAuth 2.0 authentication.
/// </summary>
public class Microsoft365Functions : SecureToolFunctionBase
{
    private readonly GraphClientFactory _graphClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly Dictionary<string, Func<IGraphToolExecutor>> ToolMap = new()
    {
        ["daisi-m365-mail-search"] = () => new MailSearchTool(),
        ["daisi-m365-mail-unread"] = () => new MailUnreadTool(),
        ["daisi-m365-mail-read"] = () => new MailReadTool(),
        ["daisi-m365-mail-send"] = () => new MailSendTool(),
        ["daisi-m365-onedrive-search"] = () => new OneDriveSearchTool(),
        ["daisi-m365-onedrive-read"] = () => new OneDriveReadTool(),
        ["daisi-m365-calendar-list"] = () => new CalendarListTool(),
        ["daisi-m365-calendar-create"] = () => new CalendarCreateTool(),
        ["daisi-m365-teams-send"] = () => new TeamsSendTool(),
    };

    public Microsoft365Functions(
        ISetupStore setupStore,
        AuthValidator authValidator,
        GraphClientFactory graphClientFactory,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<Microsoft365Functions> logger)
        : base(setupStore, authValidator, logger)
    {
        _graphClientFactory = graphClientFactory;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Returns an <see cref="OAuthHelper"/> configured for the Microsoft Identity Platform
    /// (Azure AD v2.0 endpoints) with the appropriate scopes for Microsoft 365 access.
    /// </summary>
    protected override OAuthHelper? GetOAuthHelper()
    {
        var clientId = _configuration["MicrosoftClientId"];
        var clientSecret = _configuration["MicrosoftClientSecret"];
        var tenantId = _configuration["MicrosoftTenantId"] ?? "common";

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            return null;

        var config = new OAuthConfig
        {
            AuthorizeUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize",
            TokenUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token",
            ClientId = clientId,
            ClientSecret = clientSecret,
            Scopes = ["Mail.Read", "Mail.Send", "Files.Read", "Calendars.ReadWrite", "ChannelMessage.Send", "User.Read", "offline_access"],
            RedirectUri = $"{(_configuration["BaseUrl"] ?? "https://localhost:7071")}/api/m365/auth/callback",
            AdditionalAuthParams = new Dictionary<string, string>
            {
                ["prompt"] = "consent"
            }
        };

        var httpClient = _httpClientFactory.CreateClient("OAuth");
        return new OAuthHelper(config, httpClient, Logger);
    }

    [Function("m365-install")]
    public Task<HttpResponseData> Install(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "m365/install")] HttpRequestData req)
        => HandleInstallAsync(req);

    [Function("m365-uninstall")]
    public Task<HttpResponseData> Uninstall(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "m365/uninstall")] HttpRequestData req)
        => HandleUninstallAsync(req);

    [Function("m365-configure")]
    public Task<HttpResponseData> Configure(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "m365/configure")] HttpRequestData req)
        => HandleConfigureAsync(req);

    [Function("m365-execute")]
    public Task<HttpResponseData> Execute(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "m365/execute")] HttpRequestData req)
        => HandleExecuteAsync(req);

    [Function("m365-auth-start")]
    public Task<HttpResponseData> AuthStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "m365/auth/start")] HttpRequestData req)
        => HandleAuthStartAsync(req);

    [Function("m365-auth-callback")]
    public Task<HttpResponseData> AuthCallback(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "m365/auth/callback")] HttpRequestData req)
        => HandleAuthCallbackAsync(req);

    [Function("m365-auth-status")]
    public Task<HttpResponseData> AuthStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "m365/auth/status")] HttpRequestData req)
        => HandleAuthStatusAsync(req);

    protected override async Task<ExecuteResponse> ExecuteToolAsync(
        string installId, string toolId, List<ParameterValue> parameters, Dictionary<string, string> setup)
    {
        if (!ToolMap.TryGetValue(toolId, out var toolFactory))
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = $"Unknown tool: {toolId}"
            };
        }

        // Get the OAuth access token from setup data
        var accessToken = GetAccessToken(setup, "microsoft");
        if (string.IsNullOrEmpty(accessToken))
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = "Microsoft 365 is not authenticated. Please complete the OAuth flow first."
            };
        }

        var graphClient = _graphClientFactory.CreateClient(accessToken);
        var tool = toolFactory();
        return await tool.ExecuteAsync(graphClient, parameters);
    }
}

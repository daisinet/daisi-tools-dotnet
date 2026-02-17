using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureToolProvider.Common;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Google.Tools;

namespace Daisi.SecureTools.Google;

/// <summary>
/// Azure Functions endpoints for the Google Workspace secure tool provider.
/// Supports Gmail, Drive, Calendar, and Sheets operations via Google APIs.
/// </summary>
public class GoogleFunctions : SecureToolFunctionBase
{
    private readonly GoogleServiceFactory _serviceFactory;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly Dictionary<string, Func<IGoogleToolExecutor>> ToolMap = new()
    {
        ["daisi-google-gmail-search"] = () => new GmailSearchTool(),
        ["daisi-google-gmail-unread"] = () => new GmailUnreadTool(),
        ["daisi-google-gmail-read"] = () => new GmailReadTool(),
        ["daisi-google-gmail-send"] = () => new GmailSendTool(),
        ["daisi-google-drive-search"] = () => new DriveSearchTool(),
        ["daisi-google-drive-read"] = () => new DriveReadTool(),
        ["daisi-google-calendar-list"] = () => new CalendarListTool(),
        ["daisi-google-calendar-create"] = () => new CalendarCreateTool(),
        ["daisi-google-sheets-read"] = () => new SheetsReadTool(),
        ["daisi-google-sheets-write"] = () => new SheetsWriteTool(),
    };

    public GoogleFunctions(
        ISetupStore setupStore,
        AuthValidator authValidator,
        GoogleServiceFactory serviceFactory,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<GoogleFunctions> logger)
        : base(setupStore, authValidator, logger)
    {
        _serviceFactory = serviceFactory;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Returns an OAuthHelper configured for Google OAuth 2.0 with PKCE.
    /// </summary>
    protected override OAuthHelper? GetOAuthHelper()
    {
        var clientId = _configuration["GoogleClientId"];
        var clientSecret = _configuration["GoogleClientSecret"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            return null;

        var config = new OAuthConfig
        {
            AuthorizeUrl = "https://accounts.google.com/o/oauth2/v2/auth",
            TokenUrl = "https://oauth2.googleapis.com/token",
            ClientId = clientId,
            ClientSecret = clientSecret,
            Scopes =
            [
                "https://www.googleapis.com/auth/gmail.readonly",
                "https://www.googleapis.com/auth/gmail.send",
                "https://www.googleapis.com/auth/drive.readonly",
                "https://www.googleapis.com/auth/calendar.events",
                "https://www.googleapis.com/auth/spreadsheets"
            ],
            RedirectUri = _configuration["OAuthRedirectUri"] ?? "https://localhost:7071/api/google/auth/callback",
            AdditionalAuthParams = new Dictionary<string, string>
            {
                ["access_type"] = "offline",
                ["prompt"] = "consent"
            }
        };

        var httpClient = _httpClientFactory.CreateClient();
        return new OAuthHelper(config, httpClient, Logger);
    }

    [Function("google-install")]
    public Task<HttpResponseData> Install(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "google/install")] HttpRequestData req)
        => HandleInstallAsync(req);

    [Function("google-uninstall")]
    public Task<HttpResponseData> Uninstall(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "google/uninstall")] HttpRequestData req)
        => HandleUninstallAsync(req);

    [Function("google-configure")]
    public Task<HttpResponseData> Configure(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "google/configure")] HttpRequestData req)
        => HandleConfigureAsync(req);

    [Function("google-execute")]
    public Task<HttpResponseData> Execute(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "google/execute")] HttpRequestData req)
        => HandleExecuteAsync(req);

    [Function("google-auth-start")]
    public Task<HttpResponseData> AuthStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "google/auth/start")] HttpRequestData req)
        => HandleAuthStartAsync(req);

    [Function("google-auth-callback")]
    public Task<HttpResponseData> AuthCallback(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "google/auth/callback")] HttpRequestData req)
        => HandleAuthCallbackAsync(req);

    [Function("google-auth-status")]
    public Task<HttpResponseData> AuthStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "google/auth/status")] HttpRequestData req)
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
        var accessToken = GetAccessToken(setup, "google");
        if (string.IsNullOrEmpty(accessToken))
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = "Google account is not connected. Please authenticate with Google first."
            };
        }

        var tool = toolFactory();
        return await tool.ExecuteAsync(_serviceFactory, accessToken, parameters);
    }
}

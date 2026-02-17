using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureToolProvider.Common;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;
using Daisi.SecureTools.Comms.Teams.Tools;

namespace Daisi.SecureTools.Comms.Teams;

/// <summary>
/// Azure Functions endpoints for the Microsoft Teams communications provider.
/// Sends messages to Teams chats via the Microsoft Graph API.
/// Uses OAuth with independent config (separate from M365 integration).
/// </summary>
public class TeamsFunctions : SecureToolFunctionBase
{
    private readonly SocialHttpClient _socialHttpClient;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly Dictionary<string, Func<ICommsToolExecutor>> ToolMap = new()
    {
        ["daisi-comms-teams-send"] = () => new TeamsSendTool(),
    };

    public TeamsFunctions(
        ISetupStore setupStore,
        AuthValidator authValidator,
        SocialHttpClient socialHttpClient,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<TeamsFunctions> logger)
        : base(setupStore, authValidator, logger)
    {
        _socialHttpClient = socialHttpClient;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    protected override OAuthHelper? GetOAuthHelper()
    {
        var clientId = _configuration["TeamsClientId"];
        var clientSecret = _configuration["TeamsClientSecret"];
        var tenantId = _configuration["TeamsTenantId"] ?? "common";

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            return null;

        var config = new OAuthConfig
        {
            AuthorizeUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize",
            TokenUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token",
            ClientId = clientId,
            ClientSecret = clientSecret,
            Scopes = ["Chat.ReadWrite", "ChatMessage.Send", "User.Read", "offline_access"],
            RedirectUri = _configuration["OAuthRedirectUri"] ?? "https://localhost:7071/api/comms/teams/auth/callback",
            AdditionalAuthParams = new Dictionary<string, string>()
        };

        var httpClient = _httpClientFactory.CreateClient();
        return new OAuthHelper(config, httpClient, Logger);
    }

    [Function("comms-teams-install")]
    public Task<HttpResponseData> Install(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/teams/install")] HttpRequestData req)
        => HandleInstallAsync(req);

    [Function("comms-teams-uninstall")]
    public Task<HttpResponseData> Uninstall(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/teams/uninstall")] HttpRequestData req)
        => HandleUninstallAsync(req);

    [Function("comms-teams-configure")]
    public Task<HttpResponseData> Configure(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/teams/configure")] HttpRequestData req)
        => HandleConfigureAsync(req);

    [Function("comms-teams-execute")]
    public Task<HttpResponseData> Execute(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/teams/execute")] HttpRequestData req)
        => HandleExecuteAsync(req);

    [Function("comms-teams-auth-start")]
    public Task<HttpResponseData> AuthStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/teams/auth/start")] HttpRequestData req)
        => HandleAuthStartAsync(req);

    [Function("comms-teams-auth-callback")]
    public Task<HttpResponseData> AuthCallback(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "comms/teams/auth/callback")] HttpRequestData req)
        => HandleAuthCallbackAsync(req);

    [Function("comms-teams-auth-status")]
    public Task<HttpResponseData> AuthStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/teams/auth/status")] HttpRequestData req)
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

        var accessToken = GetAccessToken(setup, "teams");
        if (string.IsNullOrEmpty(accessToken))
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = "Teams account is not connected. Please authenticate with Microsoft Teams first."
            };
        }

        var tool = toolFactory();
        return await tool.ExecuteAsync(_socialHttpClient, accessToken, parameters);
    }
}

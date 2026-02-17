using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureToolProvider.Common;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;
using Daisi.SecureTools.Comms.Slack.Tools;

namespace Daisi.SecureTools.Comms.Slack;

/// <summary>
/// Azure Functions endpoints for the Slack communications provider.
/// Sends messages to Slack channels via the Slack Web API.
/// Uses OAuth for authentication.
/// </summary>
public class SlackFunctions : SecureToolFunctionBase
{
    private readonly SocialHttpClient _socialHttpClient;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly Dictionary<string, Func<IHttpClientFactory, ICommsToolExecutor>> ToolMap = new()
    {
        ["daisi-comms-slack-send"] = hcf => new SlackSendTool(hcf),
    };

    public SlackFunctions(
        ISetupStore setupStore,
        AuthValidator authValidator,
        SocialHttpClient socialHttpClient,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<SlackFunctions> logger)
        : base(setupStore, authValidator, logger)
    {
        _socialHttpClient = socialHttpClient;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    protected override OAuthHelper? GetOAuthHelper()
    {
        var clientId = _configuration["SlackClientId"];
        var clientSecret = _configuration["SlackClientSecret"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            return null;

        var config = new OAuthConfig
        {
            AuthorizeUrl = "https://slack.com/oauth/v2/authorize",
            TokenUrl = "https://slack.com/api/oauth.v2.access",
            ClientId = clientId,
            ClientSecret = clientSecret,
            Scopes = ["chat:write", "chat:write.public", "files:write"],
            RedirectUri = _configuration["OAuthRedirectUri"] ?? "https://localhost:7071/api/comms/slack/auth/callback",
            AdditionalAuthParams = new Dictionary<string, string>()
        };

        var httpClient = _httpClientFactory.CreateClient();
        return new OAuthHelper(config, httpClient, Logger);
    }

    [Function("comms-slack-install")]
    public Task<HttpResponseData> Install(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/slack/install")] HttpRequestData req)
        => HandleInstallAsync(req);

    [Function("comms-slack-uninstall")]
    public Task<HttpResponseData> Uninstall(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/slack/uninstall")] HttpRequestData req)
        => HandleUninstallAsync(req);

    [Function("comms-slack-configure")]
    public Task<HttpResponseData> Configure(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/slack/configure")] HttpRequestData req)
        => HandleConfigureAsync(req);

    [Function("comms-slack-execute")]
    public Task<HttpResponseData> Execute(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/slack/execute")] HttpRequestData req)
        => HandleExecuteAsync(req);

    [Function("comms-slack-auth-start")]
    public Task<HttpResponseData> AuthStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/slack/auth/start")] HttpRequestData req)
        => HandleAuthStartAsync(req);

    [Function("comms-slack-auth-callback")]
    public Task<HttpResponseData> AuthCallback(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "comms/slack/auth/callback")] HttpRequestData req)
        => HandleAuthCallbackAsync(req);

    [Function("comms-slack-auth-status")]
    public Task<HttpResponseData> AuthStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/slack/auth/status")] HttpRequestData req)
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

        var accessToken = GetAccessToken(setup, "slack");
        if (string.IsNullOrEmpty(accessToken))
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = "Slack workspace is not connected. Please authenticate with Slack first."
            };
        }

        var tool = toolFactory(_httpClientFactory);
        return await tool.ExecuteAsync(_socialHttpClient, accessToken, parameters);
    }
}

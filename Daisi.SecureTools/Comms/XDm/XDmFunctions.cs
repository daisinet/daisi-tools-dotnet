using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureToolProvider.Common;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;
using Daisi.SecureTools.Comms.XDm.Tools;

namespace Daisi.SecureTools.Comms.XDm;

/// <summary>
/// Azure Functions endpoints for the X (Twitter) Direct Messages provider.
/// Sends direct messages via the X v2 API.
/// Uses OAuth with independent config (separate from social X posting).
/// </summary>
public class XDmFunctions : SecureToolFunctionBase
{
    private readonly SocialHttpClient _socialHttpClient;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly Dictionary<string, Func<IHttpClientFactory, ICommsToolExecutor>> ToolMap = new()
    {
        ["daisi-comms-xdm-send"] = hcf => new XDmSendTool(hcf),
    };

    public XDmFunctions(
        ISetupStore setupStore,
        AuthValidator authValidator,
        SocialHttpClient socialHttpClient,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<XDmFunctions> logger)
        : base(setupStore, authValidator, logger, httpClientFactory, configuration)
    {
        _socialHttpClient = socialHttpClient;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    protected override OAuthHelper? GetOAuthHelper()
    {
        var clientId = _configuration["XDmClientId"];
        var clientSecret = _configuration["XDmClientSecret"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            return null;

        var config = new OAuthConfig
        {
            AuthorizeUrl = "https://twitter.com/i/oauth2/authorize",
            TokenUrl = "https://api.twitter.com/2/oauth2/token",
            ClientId = clientId,
            ClientSecret = clientSecret,
            Scopes = ["dm.read", "dm.write", "users.read", "offline.access"],
            RedirectUri = $"{(_configuration["BaseUrl"] ?? "https://localhost:7071")}/api/comms/xdm/auth/callback",
            AdditionalAuthParams = new Dictionary<string, string>()
        };

        var httpClient = _httpClientFactory.CreateClient();
        return new OAuthHelper(config, httpClient, Logger);
    }

    [Function("comms-xdm-install")]
    public Task<HttpResponseData> Install(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/xdm/install")] HttpRequestData req)
        => HandleInstallAsync(req);

    [Function("comms-xdm-uninstall")]
    public Task<HttpResponseData> Uninstall(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/xdm/uninstall")] HttpRequestData req)
        => HandleUninstallAsync(req);

    [Function("comms-xdm-configure")]
    public Task<HttpResponseData> Configure(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/xdm/configure")] HttpRequestData req)
        => HandleConfigureAsync(req);

    [Function("comms-xdm-execute")]
    public Task<HttpResponseData> Execute(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/xdm/execute")] HttpRequestData req)
        => HandleExecuteAsync(req);

    [Function("comms-xdm-auth-start")]
    public Task<HttpResponseData> AuthStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "comms/xdm/auth/start")] HttpRequestData req)
        => HandleAuthStartAsync(req);

    [Function("comms-xdm-auth-callback")]
    public Task<HttpResponseData> AuthCallback(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "comms/xdm/auth/callback")] HttpRequestData req)
        => HandleAuthCallbackAsync(req);

    [Function("comms-xdm-auth-status")]
    public Task<HttpResponseData> AuthStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/xdm/auth/status")] HttpRequestData req)
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

        var accessToken = GetAccessToken(setup, "xdm");
        if (string.IsNullOrEmpty(accessToken))
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = "X account is not connected for DMs. Please authenticate with X first."
            };
        }

        var tool = toolFactory(_httpClientFactory);
        return await tool.ExecuteAsync(_socialHttpClient, accessToken, parameters);
    }
}

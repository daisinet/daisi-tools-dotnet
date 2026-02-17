using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureToolProvider.Common;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social.TikTok.Tools;

namespace Daisi.SecureTools.Social.TikTok;

/// <summary>
/// Azure Functions endpoints for the TikTok secure tool provider.
/// Supports publishing videos and photo posts to TikTok.
/// Note: Unaudited apps can only post as PRIVATE visibility.
/// </summary>
public class TikTokFunctions : SecureToolFunctionBase
{
    private readonly SocialHttpClient _socialHttpClient;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly Dictionary<string, Func<IHttpClientFactory, ISocialToolExecutor>> ToolMap = new()
    {
        ["daisi-social-tiktok-publish"] = hcf => new TikTokPublishTool(hcf),
    };

    public TikTokFunctions(
        ISetupStore setupStore,
        AuthValidator authValidator,
        SocialHttpClient socialHttpClient,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<TikTokFunctions> logger)
        : base(setupStore, authValidator, logger)
    {
        _socialHttpClient = socialHttpClient;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    protected override OAuthHelper? GetOAuthHelper()
    {
        var clientKey = _configuration["TikTokClientKey"];
        var clientSecret = _configuration["TikTokClientSecret"];

        if (string.IsNullOrEmpty(clientKey) || string.IsNullOrEmpty(clientSecret))
            return null;

        var config = new OAuthConfig
        {
            AuthorizeUrl = "https://www.tiktok.com/v2/auth/authorize/",
            TokenUrl = "https://open.tiktokapis.com/v2/oauth/token/",
            ClientId = clientKey,
            ClientSecret = clientSecret,
            Scopes = ["video.upload", "video.publish", "user.info.basic"],
            RedirectUri = _configuration["OAuthRedirectUri"] ?? "https://localhost:7071/api/social/tiktok/auth/callback",
            AdditionalAuthParams = new Dictionary<string, string>()
        };

        var httpClient = _httpClientFactory.CreateClient();
        return new OAuthHelper(config, httpClient, Logger);
    }

    [Function("social-tiktok-install")]
    public Task<HttpResponseData> Install(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/tiktok/install")] HttpRequestData req)
        => HandleInstallAsync(req);

    [Function("social-tiktok-uninstall")]
    public Task<HttpResponseData> Uninstall(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/tiktok/uninstall")] HttpRequestData req)
        => HandleUninstallAsync(req);

    [Function("social-tiktok-configure")]
    public Task<HttpResponseData> Configure(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/tiktok/configure")] HttpRequestData req)
        => HandleConfigureAsync(req);

    [Function("social-tiktok-execute")]
    public Task<HttpResponseData> Execute(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/tiktok/execute")] HttpRequestData req)
        => HandleExecuteAsync(req);

    [Function("social-tiktok-auth-start")]
    public Task<HttpResponseData> AuthStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "social/tiktok/auth/start")] HttpRequestData req)
        => HandleAuthStartAsync(req);

    [Function("social-tiktok-auth-callback")]
    public Task<HttpResponseData> AuthCallback(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "social/tiktok/auth/callback")] HttpRequestData req)
        => HandleAuthCallbackAsync(req);

    [Function("social-tiktok-auth-status")]
    public Task<HttpResponseData> AuthStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/tiktok/auth/status")] HttpRequestData req)
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

        var accessToken = GetAccessToken(setup, "tiktok");
        if (string.IsNullOrEmpty(accessToken))
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = "TikTok account is not connected. Please authenticate with TikTok first."
            };
        }

        var tool = toolFactory(_httpClientFactory);
        return await tool.ExecuteAsync(_socialHttpClient, accessToken, parameters);
    }
}

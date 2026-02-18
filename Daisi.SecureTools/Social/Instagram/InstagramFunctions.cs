using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureToolProvider.Common;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social.Instagram.Tools;

namespace Daisi.SecureTools.Social.Instagram;

/// <summary>
/// Azure Functions endpoints for the Instagram secure tool provider.
/// Supports publishing images and videos to Instagram Business/Creator accounts.
/// Uses Facebook Login (same OAuth provider) with Instagram-specific scopes.
/// </summary>
public class InstagramFunctions : SecureToolFunctionBase
{
    private readonly SocialHttpClient _socialHttpClient;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly Dictionary<string, Func<ISocialToolExecutor>> ToolMap = new()
    {
        ["daisi-social-instagram-publish"] = () => new InstagramPublishTool(),
    };

    public InstagramFunctions(
        ISetupStore setupStore,
        AuthValidator authValidator,
        SocialHttpClient socialHttpClient,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<InstagramFunctions> logger)
        : base(setupStore, authValidator, logger, httpClientFactory, configuration)
    {
        _socialHttpClient = socialHttpClient;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    protected override OAuthHelper? GetOAuthHelper()
    {
        var clientId = _configuration["InstagramClientId"];
        var clientSecret = _configuration["InstagramClientSecret"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            return null;

        // Instagram uses Facebook Login with Instagram-specific scopes
        var config = new OAuthConfig
        {
            AuthorizeUrl = "https://www.facebook.com/v22.0/dialog/oauth",
            TokenUrl = "https://graph.facebook.com/v22.0/oauth/access_token",
            ClientId = clientId,
            ClientSecret = clientSecret,
            Scopes = ["instagram_basic", "instagram_content_publish", "pages_read_engagement", "pages_show_list"],
            RedirectUri = $"{(_configuration["BaseUrl"] ?? "https://localhost:7071")}/api/social/instagram/auth/callback",
            AdditionalAuthParams = new Dictionary<string, string>()
        };

        var httpClient = _httpClientFactory.CreateClient();
        return new OAuthHelper(config, httpClient, Logger);
    }

    [Function("social-instagram-install")]
    public Task<HttpResponseData> Install(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/instagram/install")] HttpRequestData req)
        => HandleInstallAsync(req);

    [Function("social-instagram-uninstall")]
    public Task<HttpResponseData> Uninstall(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/instagram/uninstall")] HttpRequestData req)
        => HandleUninstallAsync(req);

    [Function("social-instagram-configure")]
    public Task<HttpResponseData> Configure(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/instagram/configure")] HttpRequestData req)
        => HandleConfigureAsync(req);

    [Function("social-instagram-execute")]
    public Task<HttpResponseData> Execute(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/instagram/execute")] HttpRequestData req)
        => HandleExecuteAsync(req);

    [Function("social-instagram-auth-start")]
    public Task<HttpResponseData> AuthStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "social/instagram/auth/start")] HttpRequestData req)
        => HandleAuthStartAsync(req);

    [Function("social-instagram-auth-callback")]
    public Task<HttpResponseData> AuthCallback(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "social/instagram/auth/callback")] HttpRequestData req)
        => HandleAuthCallbackAsync(req);

    [Function("social-instagram-auth-status")]
    public Task<HttpResponseData> AuthStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/instagram/auth/status")] HttpRequestData req)
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

        var accessToken = GetAccessToken(setup, "instagram");
        if (string.IsNullOrEmpty(accessToken))
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = "Instagram account is not connected. Please authenticate with Instagram first."
            };
        }

        var tool = toolFactory();
        return await tool.ExecuteAsync(_socialHttpClient, accessToken, parameters);
    }
}

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureToolProvider.Common;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social.Facebook.Tools;

namespace Daisi.SecureTools.Social.Facebook;

/// <summary>
/// Azure Functions endpoints for the Facebook secure tool provider.
/// Supports posting to Facebook Pages (text, photos, videos, links).
/// </summary>
public class FacebookFunctions : SecureToolFunctionBase
{
    private readonly SocialHttpClient _socialHttpClient;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly Dictionary<string, Func<ISocialToolExecutor>> ToolMap = new()
    {
        ["daisi-social-facebook-post"] = () => new FacebookPostTool(),
    };

    public FacebookFunctions(
        ISetupStore setupStore,
        AuthValidator authValidator,
        SocialHttpClient socialHttpClient,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<FacebookFunctions> logger)
        : base(setupStore, authValidator, logger)
    {
        _socialHttpClient = socialHttpClient;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    protected override OAuthHelper? GetOAuthHelper()
    {
        var clientId = _configuration["FacebookClientId"];
        var clientSecret = _configuration["FacebookClientSecret"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            return null;

        var config = new OAuthConfig
        {
            AuthorizeUrl = "https://www.facebook.com/v22.0/dialog/oauth",
            TokenUrl = "https://graph.facebook.com/v22.0/oauth/access_token",
            ClientId = clientId,
            ClientSecret = clientSecret,
            Scopes = ["pages_manage_posts", "pages_read_engagement", "pages_show_list"],
            RedirectUri = $"{(_configuration["BaseUrl"] ?? "https://localhost:7071")}/api/social/facebook/auth/callback",
            AdditionalAuthParams = new Dictionary<string, string>()
        };

        var httpClient = _httpClientFactory.CreateClient();
        return new OAuthHelper(config, httpClient, Logger);
    }

    [Function("social-facebook-install")]
    public Task<HttpResponseData> Install(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/facebook/install")] HttpRequestData req)
        => HandleInstallAsync(req);

    [Function("social-facebook-uninstall")]
    public Task<HttpResponseData> Uninstall(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/facebook/uninstall")] HttpRequestData req)
        => HandleUninstallAsync(req);

    [Function("social-facebook-configure")]
    public Task<HttpResponseData> Configure(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/facebook/configure")] HttpRequestData req)
        => HandleConfigureAsync(req);

    [Function("social-facebook-configure-status")]
    public Task<HttpResponseData> ConfigureStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/facebook/configure/status")] HttpRequestData req)
        => HandleConfigureStatusAsync(req);

    [Function("social-facebook-execute")]
    public Task<HttpResponseData> Execute(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/facebook/execute")] HttpRequestData req)
        => HandleExecuteAsync(req);

    [Function("social-facebook-auth-start")]
    public Task<HttpResponseData> AuthStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "social/facebook/auth/start")] HttpRequestData req)
        => HandleAuthStartAsync(req);

    [Function("social-facebook-auth-callback")]
    public Task<HttpResponseData> AuthCallback(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "social/facebook/auth/callback")] HttpRequestData req)
        => HandleAuthCallbackAsync(req);

    [Function("social-facebook-auth-status")]
    public Task<HttpResponseData> AuthStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/facebook/auth/status")] HttpRequestData req)
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

        var accessToken = GetAccessToken(setup, "facebook");
        if (string.IsNullOrEmpty(accessToken))
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = "Facebook account is not connected. Please authenticate with Facebook first."
            };
        }

        var tool = toolFactory();
        return await tool.ExecuteAsync(_socialHttpClient, accessToken, parameters);
    }
}

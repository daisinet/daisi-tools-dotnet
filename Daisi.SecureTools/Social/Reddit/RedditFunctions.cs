using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureToolProvider.Common;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social.Reddit.Tools;

namespace Daisi.SecureTools.Social.Reddit;

/// <summary>
/// Azure Functions endpoints for the Reddit secure tool provider.
/// Supports submitting text posts, link posts, and image posts to subreddits.
/// </summary>
public class RedditFunctions : SecureToolFunctionBase
{
    private readonly SocialHttpClient _socialHttpClient;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly Dictionary<string, Func<ISocialToolExecutor>> ToolMap = new()
    {
        ["daisi-social-reddit-submit"] = () => new RedditSubmitTool(),
    };

    public RedditFunctions(
        ISetupStore setupStore,
        AuthValidator authValidator,
        SocialHttpClient socialHttpClient,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<RedditFunctions> logger)
        : base(setupStore, authValidator, logger)
    {
        _socialHttpClient = socialHttpClient;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    protected override OAuthHelper? GetOAuthHelper()
    {
        var clientId = _configuration["RedditClientId"];
        var clientSecret = _configuration["RedditClientSecret"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            return null;

        var config = new OAuthConfig
        {
            AuthorizeUrl = "https://www.reddit.com/api/v1/authorize",
            TokenUrl = "https://www.reddit.com/api/v1/access_token",
            ClientId = clientId,
            ClientSecret = clientSecret,
            Scopes = ["submit", "identity"],
            RedirectUri = $"{(_configuration["BaseUrl"] ?? "https://localhost:7071")}/api/social/reddit/auth/callback",
            AdditionalAuthParams = new Dictionary<string, string>
            {
                ["duration"] = "permanent"
            }
        };

        var httpClient = _httpClientFactory.CreateClient();
        return new OAuthHelper(config, httpClient, Logger);
    }

    [Function("social-reddit-install")]
    public Task<HttpResponseData> Install(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/reddit/install")] HttpRequestData req)
        => HandleInstallAsync(req);

    [Function("social-reddit-uninstall")]
    public Task<HttpResponseData> Uninstall(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/reddit/uninstall")] HttpRequestData req)
        => HandleUninstallAsync(req);

    [Function("social-reddit-configure")]
    public Task<HttpResponseData> Configure(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/reddit/configure")] HttpRequestData req)
        => HandleConfigureAsync(req);

    [Function("social-reddit-configure-status")]
    public Task<HttpResponseData> ConfigureStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/reddit/configure/status")] HttpRequestData req)
        => HandleConfigureStatusAsync(req);

    [Function("social-reddit-execute")]
    public Task<HttpResponseData> Execute(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/reddit/execute")] HttpRequestData req)
        => HandleExecuteAsync(req);

    [Function("social-reddit-auth-start")]
    public Task<HttpResponseData> AuthStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "social/reddit/auth/start")] HttpRequestData req)
        => HandleAuthStartAsync(req);

    [Function("social-reddit-auth-callback")]
    public Task<HttpResponseData> AuthCallback(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "social/reddit/auth/callback")] HttpRequestData req)
        => HandleAuthCallbackAsync(req);

    [Function("social-reddit-auth-status")]
    public Task<HttpResponseData> AuthStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/reddit/auth/status")] HttpRequestData req)
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

        var accessToken = GetAccessToken(setup, "reddit");
        if (string.IsNullOrEmpty(accessToken))
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = "Reddit account is not connected. Please authenticate with Reddit first."
            };
        }

        var tool = toolFactory();
        return await tool.ExecuteAsync(_socialHttpClient, accessToken, parameters);
    }
}

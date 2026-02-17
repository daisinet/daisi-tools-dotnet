using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureToolProvider.Common;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social.X.Tools;

namespace Daisi.SecureTools.Social.X;

/// <summary>
/// Azure Functions endpoints for the X (Twitter) secure tool provider.
/// Supports posting tweets with optional media, replies, and quote tweets.
/// </summary>
public class XFunctions : SecureToolFunctionBase
{
    private readonly SocialHttpClient _socialHttpClient;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly Dictionary<string, Func<IHttpClientFactory, ISocialToolExecutor>> ToolMap = new()
    {
        ["daisi-social-x-post"] = hcf => new XPostTool(hcf),
    };

    public XFunctions(
        ISetupStore setupStore,
        AuthValidator authValidator,
        SocialHttpClient socialHttpClient,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<XFunctions> logger)
        : base(setupStore, authValidator, logger)
    {
        _socialHttpClient = socialHttpClient;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    protected override OAuthHelper? GetOAuthHelper()
    {
        var clientId = _configuration["XClientId"];
        var clientSecret = _configuration["XClientSecret"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            return null;

        var config = new OAuthConfig
        {
            AuthorizeUrl = "https://twitter.com/i/oauth2/authorize",
            TokenUrl = "https://api.twitter.com/2/oauth2/token",
            ClientId = clientId,
            ClientSecret = clientSecret,
            Scopes = ["tweet.read", "tweet.write", "users.read", "media.write", "offline.access"],
            RedirectUri = $"{(_configuration["BaseUrl"] ?? "https://localhost:7071")}/api/social/x/auth/callback",
            AdditionalAuthParams = new Dictionary<string, string>()
        };

        var httpClient = _httpClientFactory.CreateClient();
        return new OAuthHelper(config, httpClient, Logger);
    }

    [Function("social-x-install")]
    public Task<HttpResponseData> Install(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/x/install")] HttpRequestData req)
        => HandleInstallAsync(req);

    [Function("social-x-uninstall")]
    public Task<HttpResponseData> Uninstall(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/x/uninstall")] HttpRequestData req)
        => HandleUninstallAsync(req);

    [Function("social-x-configure")]
    public Task<HttpResponseData> Configure(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/x/configure")] HttpRequestData req)
        => HandleConfigureAsync(req);

    [Function("social-x-execute")]
    public Task<HttpResponseData> Execute(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/x/execute")] HttpRequestData req)
        => HandleExecuteAsync(req);

    [Function("social-x-auth-start")]
    public Task<HttpResponseData> AuthStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "social/x/auth/start")] HttpRequestData req)
        => HandleAuthStartAsync(req);

    [Function("social-x-auth-callback")]
    public Task<HttpResponseData> AuthCallback(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "social/x/auth/callback")] HttpRequestData req)
        => HandleAuthCallbackAsync(req);

    [Function("social-x-auth-status")]
    public Task<HttpResponseData> AuthStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/x/auth/status")] HttpRequestData req)
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

        var accessToken = GetAccessToken(setup, "x");
        if (string.IsNullOrEmpty(accessToken))
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = "X account is not connected. Please authenticate with X first."
            };
        }

        var tool = toolFactory(_httpClientFactory);
        return await tool.ExecuteAsync(_socialHttpClient, accessToken, parameters);
    }
}

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureToolProvider.Common;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social.LinkedIn.Tools;

namespace Daisi.SecureTools.Social.LinkedIn;

/// <summary>
/// Azure Functions endpoints for the LinkedIn secure tool provider.
/// Supports posting text and image content to LinkedIn profiles.
/// </summary>
public class LinkedInFunctions : SecureToolFunctionBase
{
    private readonly SocialHttpClient _socialHttpClient;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly Dictionary<string, Func<IHttpClientFactory, ISocialToolExecutor>> ToolMap = new()
    {
        ["daisi-social-linkedin-post"] = hcf => new LinkedInPostTool(hcf),
    };

    public LinkedInFunctions(
        ISetupStore setupStore,
        AuthValidator authValidator,
        SocialHttpClient socialHttpClient,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<LinkedInFunctions> logger)
        : base(setupStore, authValidator, logger)
    {
        _socialHttpClient = socialHttpClient;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    protected override OAuthHelper? GetOAuthHelper()
    {
        var clientId = _configuration["LinkedInClientId"];
        var clientSecret = _configuration["LinkedInClientSecret"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            return null;

        var config = new OAuthConfig
        {
            AuthorizeUrl = "https://www.linkedin.com/oauth/v2/authorization",
            TokenUrl = "https://www.linkedin.com/oauth/v2/accessToken",
            ClientId = clientId,
            ClientSecret = clientSecret,
            Scopes = ["w_member_social", "openid", "profile"],
            RedirectUri = _configuration["OAuthRedirectUri"] ?? "https://localhost:7071/api/social/linkedin/auth/callback",
            AdditionalAuthParams = new Dictionary<string, string>()
        };

        var httpClient = _httpClientFactory.CreateClient();
        return new OAuthHelper(config, httpClient, Logger);
    }

    [Function("social-linkedin-install")]
    public Task<HttpResponseData> Install(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/linkedin/install")] HttpRequestData req)
        => HandleInstallAsync(req);

    [Function("social-linkedin-uninstall")]
    public Task<HttpResponseData> Uninstall(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/linkedin/uninstall")] HttpRequestData req)
        => HandleUninstallAsync(req);

    [Function("social-linkedin-configure")]
    public Task<HttpResponseData> Configure(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/linkedin/configure")] HttpRequestData req)
        => HandleConfigureAsync(req);

    [Function("social-linkedin-execute")]
    public Task<HttpResponseData> Execute(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/linkedin/execute")] HttpRequestData req)
        => HandleExecuteAsync(req);

    [Function("social-linkedin-auth-start")]
    public Task<HttpResponseData> AuthStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "social/linkedin/auth/start")] HttpRequestData req)
        => HandleAuthStartAsync(req);

    [Function("social-linkedin-auth-callback")]
    public Task<HttpResponseData> AuthCallback(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "social/linkedin/auth/callback")] HttpRequestData req)
        => HandleAuthCallbackAsync(req);

    [Function("social-linkedin-auth-status")]
    public Task<HttpResponseData> AuthStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "social/linkedin/auth/status")] HttpRequestData req)
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

        var accessToken = GetAccessToken(setup, "linkedin");
        if (string.IsNullOrEmpty(accessToken))
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = "LinkedIn account is not connected. Please authenticate with LinkedIn first."
            };
        }

        var tool = toolFactory(_httpClientFactory);
        return await tool.ExecuteAsync(_socialHttpClient, accessToken, parameters);
    }
}

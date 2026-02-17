using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureToolProvider.Common;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;
using Daisi.SecureTools.Comms.WhatsApp.Tools;

namespace Daisi.SecureTools.Comms.WhatsApp;

/// <summary>
/// Azure Functions endpoints for the WhatsApp communications provider.
/// Sends messages via the Meta Cloud API (WhatsApp Business Platform).
/// Uses OAuth via Facebook Login for authentication.
/// </summary>
public class WhatsAppFunctions : SecureToolFunctionBase
{
    private readonly SocialHttpClient _socialHttpClient;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly Dictionary<string, Func<ICommsToolExecutor>> ToolMap = new()
    {
        ["daisi-comms-whatsapp-send"] = () => new WhatsAppSendTool(),
    };

    public WhatsAppFunctions(
        ISetupStore setupStore,
        AuthValidator authValidator,
        SocialHttpClient socialHttpClient,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<WhatsAppFunctions> logger)
        : base(setupStore, authValidator, logger)
    {
        _socialHttpClient = socialHttpClient;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    protected override OAuthHelper? GetOAuthHelper()
    {
        var clientId = _configuration["WhatsAppClientId"];
        var clientSecret = _configuration["WhatsAppClientSecret"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            return null;

        var config = new OAuthConfig
        {
            AuthorizeUrl = "https://www.facebook.com/v22.0/dialog/oauth",
            TokenUrl = "https://graph.facebook.com/v22.0/oauth/access_token",
            ClientId = clientId,
            ClientSecret = clientSecret,
            Scopes = ["whatsapp_business_management", "whatsapp_business_messaging"],
            RedirectUri = _configuration["OAuthRedirectUri"] ?? "https://localhost:7071/api/comms/whatsapp/auth/callback",
            AdditionalAuthParams = new Dictionary<string, string>()
        };

        var httpClient = _httpClientFactory.CreateClient();
        return new OAuthHelper(config, httpClient, Logger);
    }

    [Function("comms-whatsapp-install")]
    public Task<HttpResponseData> Install(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/whatsapp/install")] HttpRequestData req)
        => HandleInstallAsync(req);

    [Function("comms-whatsapp-uninstall")]
    public Task<HttpResponseData> Uninstall(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/whatsapp/uninstall")] HttpRequestData req)
        => HandleUninstallAsync(req);

    [Function("comms-whatsapp-configure")]
    public Task<HttpResponseData> Configure(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/whatsapp/configure")] HttpRequestData req)
        => HandleConfigureAsync(req);

    [Function("comms-whatsapp-execute")]
    public Task<HttpResponseData> Execute(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/whatsapp/execute")] HttpRequestData req)
        => HandleExecuteAsync(req);

    [Function("comms-whatsapp-auth-start")]
    public Task<HttpResponseData> AuthStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "comms/whatsapp/auth/start")] HttpRequestData req)
        => HandleAuthStartAsync(req);

    [Function("comms-whatsapp-auth-callback")]
    public Task<HttpResponseData> AuthCallback(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "comms/whatsapp/auth/callback")] HttpRequestData req)
        => HandleAuthCallbackAsync(req);

    [Function("comms-whatsapp-auth-status")]
    public Task<HttpResponseData> AuthStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/whatsapp/auth/status")] HttpRequestData req)
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

        var accessToken = GetAccessToken(setup, "whatsapp");
        if (string.IsNullOrEmpty(accessToken))
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = "WhatsApp account is not connected. Please authenticate with WhatsApp first."
            };
        }

        var tool = toolFactory();
        return await tool.ExecuteAsync(_socialHttpClient, accessToken, parameters);
    }
}

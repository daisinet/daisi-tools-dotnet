using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SecureToolProvider.Common;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;
using Daisi.SecureTools.Comms.Telegram.Tools;

namespace Daisi.SecureTools.Comms.Telegram;

/// <summary>
/// Azure Functions endpoints for the Telegram communications provider.
/// Supports sending messages, photos, documents, and videos via Bot API.
/// Uses API key authentication (bot token from BotFather) — no OAuth.
/// </summary>
public class TelegramFunctions : SecureToolFunctionBase
{
    private readonly SocialHttpClient _socialHttpClient;
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly Dictionary<string, Func<IHttpClientFactory, ICommsToolExecutor>> ToolMap = new()
    {
        ["daisi-comms-telegram-send"] = hcf => new TelegramSendTool(hcf),
    };

    public TelegramFunctions(
        ISetupStore setupStore,
        AuthValidator authValidator,
        SocialHttpClient socialHttpClient,
        IHttpClientFactory httpClientFactory,
        ILogger<TelegramFunctions> logger)
        : base(setupStore, authValidator, logger)
    {
        _socialHttpClient = socialHttpClient;
        _httpClientFactory = httpClientFactory;
    }

    protected override OAuthHelper? GetOAuthHelper() => null;

    [Function("comms-telegram-install")]
    public Task<HttpResponseData> Install(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/telegram/install")] HttpRequestData req)
        => HandleInstallAsync(req);

    [Function("comms-telegram-uninstall")]
    public Task<HttpResponseData> Uninstall(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/telegram/uninstall")] HttpRequestData req)
        => HandleUninstallAsync(req);

    [Function("comms-telegram-configure")]
    public Task<HttpResponseData> Configure(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/telegram/configure")] HttpRequestData req)
        => HandleConfigureAsync(req);

    [Function("comms-telegram-execute")]
    public Task<HttpResponseData> Execute(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/telegram/execute")] HttpRequestData req)
        => HandleExecuteAsync(req);

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

        var botToken = setup.GetValueOrDefault("botToken");
        if (string.IsNullOrEmpty(botToken))
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = "Telegram bot token is not configured. Please configure the tool with a 'botToken' from BotFather."
            };
        }

        var tool = toolFactory(_httpClientFactory);
        return await tool.ExecuteAsync(_socialHttpClient, botToken, parameters);
    }
}

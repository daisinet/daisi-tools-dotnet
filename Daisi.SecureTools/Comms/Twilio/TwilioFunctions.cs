using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureToolProvider.Common;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;
using Daisi.SecureTools.Comms.Twilio.Tools;

namespace Daisi.SecureTools.Comms.Twilio;

/// <summary>
/// Azure Functions endpoints for the Twilio communications provider.
/// Supports SMS, voice calls, and email (via SendGrid).
/// Uses API key authentication (Account SID + Auth Token) — no OAuth.
/// </summary>
public class TwilioFunctions : SecureToolFunctionBase
{
    private readonly SocialHttpClient _socialHttpClient;
    private readonly IConfiguration _configuration;

    private static readonly Dictionary<string, Func<ICommsToolExecutor>> ToolMap = new()
    {
        ["daisi-comms-twilio-sms"] = () => new TwilioSmsTool(),
        ["daisi-comms-twilio-voice"] = () => new TwilioVoiceTool(),
        ["daisi-comms-twilio-email"] = () => new TwilioEmailTool(),
    };

    public TwilioFunctions(
        ISetupStore setupStore,
        AuthValidator authValidator,
        SocialHttpClient socialHttpClient,
        IConfiguration configuration,
        ILogger<TwilioFunctions> logger)
        : base(setupStore, authValidator, logger)
    {
        _socialHttpClient = socialHttpClient;
        _configuration = configuration;
    }

    protected override OAuthHelper? GetOAuthHelper() => null;

    [Function("comms-twilio-install")]
    public Task<HttpResponseData> Install(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/twilio/install")] HttpRequestData req)
        => HandleInstallAsync(req);

    [Function("comms-twilio-uninstall")]
    public Task<HttpResponseData> Uninstall(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/twilio/uninstall")] HttpRequestData req)
        => HandleUninstallAsync(req);

    [Function("comms-twilio-configure")]
    public Task<HttpResponseData> Configure(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/twilio/configure")] HttpRequestData req)
        => HandleConfigureAsync(req);

    [Function("comms-twilio-execute")]
    public Task<HttpResponseData> Execute(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "comms/twilio/execute")] HttpRequestData req)
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

        // For email tool, use SendGrid API key
        if (toolId == "daisi-comms-twilio-email")
        {
            var sendGridApiKey = setup.GetValueOrDefault("sendGridApiKey")
                ?? _configuration["TwilioSendGridApiKey"];
            if (string.IsNullOrEmpty(sendGridApiKey))
            {
                return new ExecuteResponse
                {
                    Success = false,
                    ErrorMessage = "SendGrid API key is not configured. Please configure the tool with a 'sendGridApiKey'."
                };
            }

            var tool = toolFactory();
            return await tool.ExecuteAsync(_socialHttpClient, sendGridApiKey, parameters);
        }

        // For SMS and Voice, use Twilio Account SID + Auth Token as Basic auth
        var accountSid = setup.GetValueOrDefault("accountSid")
            ?? _configuration["TwilioAccountSid"];
        var authToken = setup.GetValueOrDefault("authToken")
            ?? _configuration["TwilioAuthToken"];

        if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken))
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = "Twilio credentials are not configured. Please configure the tool with 'accountSid' and 'authToken'."
            };
        }

        // Set 'from' default from setup if not in parameters
        var fromParam = parameters.FirstOrDefault(p => p.Name == "from");
        if (fromParam is null || string.IsNullOrEmpty(fromParam.Value))
        {
            var defaultFrom = setup.GetValueOrDefault("fromPhone");
            if (!string.IsNullOrEmpty(defaultFrom))
                parameters.Add(new ParameterValue { Name = "from", Value = defaultFrom });
        }

        var basicAuth = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{accountSid}:{authToken}"));

        var smsVoiceTool = toolFactory();
        return await smsVoiceTool.ExecuteAsync(_socialHttpClient, basicAuth, parameters);
    }
}

using System.Text.Json;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;

namespace Daisi.SecureTools.Comms.Twilio.Tools;

/// <summary>
/// Initiate a voice call via the Twilio REST API.
/// </summary>
public class TwilioVoiceTool : ICommsToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(
        SocialHttpClient httpClient, string accessToken, List<ParameterValue> parameters)
    {
        var to = parameters.FirstOrDefault(p => p.Name == "to")?.Value;
        if (string.IsNullOrEmpty(to))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'to' parameter is required." };

        var twimlUrl = parameters.FirstOrDefault(p => p.Name == "twimlUrl")?.Value;
        if (string.IsNullOrEmpty(twimlUrl))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'twimlUrl' parameter is required." };

        var from = parameters.FirstOrDefault(p => p.Name == "from")?.Value;

        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(accessToken));
        var accountSid = decoded.Split(':')[0];

        var formFields = new Dictionary<string, string>
        {
            ["To"] = to,
            ["Url"] = twimlUrl
        };

        if (!string.IsNullOrEmpty(from))
            formFields["From"] = from;

        var result = await httpClient.PostFormUrlEncodedAsync(
            $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Calls.json",
            formFields,
            basicAuth: accessToken);

        var sid = result.TryGetProperty("sid", out var s) ? s.GetString() : null;
        var status = result.TryGetProperty("status", out var st) ? st.GetString() : null;

        return new ExecuteResponse
        {
            Success = true,
            Output = JsonSerializer.Serialize(new { sid, status, to, from }, SocialHttpClient.JsonOptions),
            OutputFormat = "json",
            OutputMessage = $"Voice call initiated to {to}."
        };
    }
}

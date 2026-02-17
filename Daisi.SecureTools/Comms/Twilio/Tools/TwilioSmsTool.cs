using System.Text.Json;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;

namespace Daisi.SecureTools.Comms.Twilio.Tools;

/// <summary>
/// Send an SMS message via the Twilio REST API.
/// </summary>
public class TwilioSmsTool : ICommsToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(
        SocialHttpClient httpClient, string accessToken, List<ParameterValue> parameters)
    {
        // accessToken is the base64-encoded "AccountSid:AuthToken" for Basic auth
        var to = parameters.FirstOrDefault(p => p.Name == "to")?.Value;
        if (string.IsNullOrEmpty(to))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'to' parameter is required." };

        var body = parameters.FirstOrDefault(p => p.Name == "body")?.Value;
        if (string.IsNullOrEmpty(body))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'body' parameter is required." };

        var from = parameters.FirstOrDefault(p => p.Name == "from")?.Value;
        var mediaUrl = parameters.FirstOrDefault(p => p.Name == "mediaUrl")?.Value;

        // Extract account SID from the Basic auth credentials
        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(accessToken));
        var accountSid = decoded.Split(':')[0];

        var formFields = new Dictionary<string, string>
        {
            ["To"] = to,
            ["Body"] = body
        };

        if (!string.IsNullOrEmpty(from))
            formFields["From"] = from;

        if (!string.IsNullOrEmpty(mediaUrl))
            formFields["MediaUrl"] = mediaUrl;

        var result = await httpClient.PostFormUrlEncodedAsync(
            $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json",
            formFields,
            basicAuth: accessToken);

        var sid = result.TryGetProperty("sid", out var s) ? s.GetString() : null;
        var status = result.TryGetProperty("status", out var st) ? st.GetString() : null;

        return new ExecuteResponse
        {
            Success = true,
            Output = JsonSerializer.Serialize(new { sid, status, to, from }, SocialHttpClient.JsonOptions),
            OutputFormat = "json",
            OutputMessage = $"SMS sent to {to}."
        };
    }
}

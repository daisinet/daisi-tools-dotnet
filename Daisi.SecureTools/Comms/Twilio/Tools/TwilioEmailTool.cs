using System.Text.Json;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Social;

namespace Daisi.SecureTools.Comms.Twilio.Tools;

/// <summary>
/// Send an email via the SendGrid API (Twilio's email service).
/// </summary>
public class TwilioEmailTool : ICommsToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(
        SocialHttpClient httpClient, string accessToken, List<ParameterValue> parameters)
    {
        // accessToken here is the SendGrid API key
        var to = parameters.FirstOrDefault(p => p.Name == "to")?.Value;
        if (string.IsNullOrEmpty(to))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'to' parameter is required." };

        var subject = parameters.FirstOrDefault(p => p.Name == "subject")?.Value;
        if (string.IsNullOrEmpty(subject))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'subject' parameter is required." };

        var body = parameters.FirstOrDefault(p => p.Name == "body")?.Value;
        if (string.IsNullOrEmpty(body))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'body' parameter is required." };

        var from = parameters.FirstOrDefault(p => p.Name == "from")?.Value ?? "noreply@example.com";
        var isHtml = parameters.FirstOrDefault(p => p.Name == "isHtml")?.Value;
        var contentType = string.Equals(isHtml, "true", StringComparison.OrdinalIgnoreCase)
            ? "text/html" : "text/plain";

        var payload = new
        {
            personalizations = new[]
            {
                new { to = new[] { new { email = to } } }
            },
            from = new { email = from },
            subject,
            content = new[]
            {
                new { type = contentType, value = body }
            }
        };

        await httpClient.PostJsonAsync("https://api.sendgrid.com/v3/mail/send", accessToken, payload);

        return new ExecuteResponse
        {
            Success = true,
            Output = JsonSerializer.Serialize(new { to, subject, from, contentType }, SocialHttpClient.JsonOptions),
            OutputFormat = "json",
            OutputMessage = $"Email sent to {to}."
        };
    }
}

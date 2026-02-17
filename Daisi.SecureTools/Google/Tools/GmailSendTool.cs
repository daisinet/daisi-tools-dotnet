using System.Text;
using System.Text.Json;
using Google.Apis.Gmail.v1.Data;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Google.Tools;

/// <summary>
/// Send an email via Gmail. Builds a raw MIME message from parameters.
/// </summary>
public class GmailSendTool : IGoogleToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(
        GoogleServiceFactory serviceFactory, string accessToken, List<ParameterValue> parameters)
    {
        var to = parameters.FirstOrDefault(p => p.Name == "to")?.Value;
        var subject = parameters.FirstOrDefault(p => p.Name == "subject")?.Value;
        var body = parameters.FirstOrDefault(p => p.Name == "body")?.Value;

        if (string.IsNullOrEmpty(to))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'to' parameter is required." };
        if (string.IsNullOrEmpty(subject))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'subject' parameter is required." };
        if (string.IsNullOrEmpty(body))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'body' parameter is required." };

        var cc = parameters.FirstOrDefault(p => p.Name == "cc")?.Value;
        var bcc = parameters.FirstOrDefault(p => p.Name == "bcc")?.Value;

        var mimeMessage = BuildMimeMessage(to, subject, body, cc, bcc);
        var rawMessage = Base64UrlEncode(mimeMessage);

        var service = serviceFactory.CreateGmailService(accessToken);

        var message = new Message { Raw = rawMessage };
        var sendRequest = service.Users.Messages.Send(message, "me");
        var sent = await sendRequest.ExecuteAsync();

        var result = new
        {
            id = sent.Id,
            threadId = sent.ThreadId,
            to,
            subject,
            status = "sent"
        };

        var output = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });

        return new ExecuteResponse
        {
            Success = true,
            Output = output,
            OutputFormat = "json",
            OutputMessage = $"Email sent to {to}."
        };
    }

    internal static string BuildMimeMessage(string to, string subject, string body, string? cc, string? bcc)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"To: {to}");
        if (!string.IsNullOrEmpty(cc))
            sb.AppendLine($"Cc: {cc}");
        if (!string.IsNullOrEmpty(bcc))
            sb.AppendLine($"Bcc: {bcc}");
        sb.AppendLine($"Subject: {subject}");
        sb.AppendLine("Content-Type: text/plain; charset=utf-8");
        sb.AppendLine();
        sb.Append(body);
        return sb.ToString();
    }

    internal static string Base64UrlEncode(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}

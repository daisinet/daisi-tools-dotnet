using System.Text.Json;
using Microsoft.Graph;
using Microsoft.Graph.Me.SendMail;
using Microsoft.Graph.Models;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Microsoft365.Tools;

/// <summary>
/// Send an email via Outlook using the Microsoft Graph API.
/// Builds a message from to, subject, body, and optional cc/bcc parameters.
/// </summary>
public class MailSendTool : IGraphToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(GraphServiceClient graphClient, List<ParameterValue> parameters)
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

        var message = new Message
        {
            Subject = subject,
            Body = new ItemBody
            {
                ContentType = BodyType.Text,
                Content = body
            },
            ToRecipients = ParseRecipients(to),
        };

        if (!string.IsNullOrEmpty(cc))
            message.CcRecipients = ParseRecipients(cc);

        if (!string.IsNullOrEmpty(bcc))
            message.BccRecipients = ParseRecipients(bcc);

        await graphClient.Me.SendMail.PostAsync(new SendMailPostRequestBody
        {
            Message = message,
            SaveToSentItems = true
        });

        var result = new
        {
            sent = true,
            to,
            subject,
            cc = cc ?? "",
            bcc = bcc ?? ""
        };

        return new ExecuteResponse
        {
            Success = true,
            Output = JsonSerializer.Serialize(result),
            OutputFormat = "json",
            OutputMessage = $"Email sent to {to}: {subject}"
        };
    }

    /// <summary>
    /// Parse a comma-separated list of email addresses into recipients.
    /// </summary>
    private static List<Recipient> ParseRecipients(string emailList)
    {
        return emailList
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(email => new Recipient
            {
                EmailAddress = new EmailAddress { Address = email }
            })
            .ToList();
    }
}

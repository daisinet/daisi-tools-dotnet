using System.Text.Json;
using Microsoft.Graph;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Microsoft365.Tools;

/// <summary>
/// Read a full email by message ID. Returns subject, from, to, date, and body.
/// Prefers text body format when available.
/// </summary>
public class MailReadTool : IGraphToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(GraphServiceClient graphClient, List<ParameterValue> parameters)
    {
        var messageId = parameters.FirstOrDefault(p => p.Name == "messageId")?.Value;
        if (string.IsNullOrEmpty(messageId))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'messageId' parameter is required." };

        var message = await graphClient.Me.Messages[messageId].GetAsync(config =>
        {
            config.QueryParameters.Select = ["subject", "from", "toRecipients", "ccRecipients", "receivedDateTime", "body"];
            config.Headers.Add("Prefer", "outlook.body-content-type=\"text\"");
        });

        if (message is null)
            return new ExecuteResponse { Success = false, ErrorMessage = $"Message '{messageId}' not found." };

        var result = new
        {
            id = message.Id,
            subject = message.Subject,
            from = message.From?.EmailAddress?.Address,
            fromName = message.From?.EmailAddress?.Name,
            to = message.ToRecipients?.Select(r => new
            {
                address = r.EmailAddress?.Address,
                name = r.EmailAddress?.Name
            }),
            cc = message.CcRecipients?.Select(r => new
            {
                address = r.EmailAddress?.Address,
                name = r.EmailAddress?.Name
            }),
            date = message.ReceivedDateTime?.ToString("O"),
            bodyType = message.Body?.ContentType?.ToString(),
            body = message.Body?.Content
        };

        return new ExecuteResponse
        {
            Success = true,
            Output = JsonSerializer.Serialize(result),
            OutputFormat = "json",
            OutputMessage = $"Read message: {message.Subject}"
        };
    }
}

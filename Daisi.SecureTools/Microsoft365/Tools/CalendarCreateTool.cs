using System.Text.Json;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Microsoft365.Tools;

/// <summary>
/// Create a calendar event in the user's primary calendar.
/// Builds an Event object from subject, start, end, optional body and attendees.
/// </summary>
public class CalendarCreateTool : IGraphToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(GraphServiceClient graphClient, List<ParameterValue> parameters)
    {
        var subject = parameters.FirstOrDefault(p => p.Name == "subject")?.Value;
        var start = parameters.FirstOrDefault(p => p.Name == "start")?.Value;
        var end = parameters.FirstOrDefault(p => p.Name == "end")?.Value;

        if (string.IsNullOrEmpty(subject))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'subject' parameter is required." };
        if (string.IsNullOrEmpty(start))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'start' parameter is required." };
        if (string.IsNullOrEmpty(end))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'end' parameter is required." };

        var body = parameters.FirstOrDefault(p => p.Name == "body")?.Value;
        var attendees = parameters.FirstOrDefault(p => p.Name == "attendees")?.Value;
        var timeZone = parameters.FirstOrDefault(p => p.Name == "timeZone")?.Value ?? "UTC";
        var location = parameters.FirstOrDefault(p => p.Name == "location")?.Value;

        var newEvent = new Event
        {
            Subject = subject,
            Start = new DateTimeTimeZone
            {
                DateTime = start,
                TimeZone = timeZone
            },
            End = new DateTimeTimeZone
            {
                DateTime = end,
                TimeZone = timeZone
            }
        };

        if (!string.IsNullOrEmpty(body))
        {
            newEvent.Body = new ItemBody
            {
                ContentType = BodyType.Text,
                Content = body
            };
        }

        if (!string.IsNullOrEmpty(location))
        {
            newEvent.Location = new Location
            {
                DisplayName = location
            };
        }

        if (!string.IsNullOrEmpty(attendees))
        {
            newEvent.Attendees = attendees
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(email => new Attendee
                {
                    EmailAddress = new EmailAddress { Address = email },
                    Type = AttendeeType.Required
                })
                .ToList();
        }

        var created = await graphClient.Me.Calendar.Events.PostAsync(newEvent);

        var result = new
        {
            id = created?.Id,
            subject = created?.Subject,
            start = created?.Start?.DateTime,
            startTimeZone = created?.Start?.TimeZone,
            end = created?.End?.DateTime,
            endTimeZone = created?.End?.TimeZone,
            location = created?.Location?.DisplayName,
            webLink = created?.WebLink
        };

        return new ExecuteResponse
        {
            Success = true,
            Output = JsonSerializer.Serialize(result),
            OutputFormat = "json",
            OutputMessage = $"Created calendar event: {subject}"
        };
    }
}

using System.Text.Json;
using Google.Apis.Calendar.v3.Data;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Google.Tools;

/// <summary>
/// Create a new event on Google Calendar.
/// Supports summary, start/end times, description, and attendees.
/// </summary>
public class CalendarCreateTool : IGoogleToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(
        GoogleServiceFactory serviceFactory, string accessToken, List<ParameterValue> parameters)
    {
        var summary = parameters.FirstOrDefault(p => p.Name == "summary")?.Value;
        var startStr = parameters.FirstOrDefault(p => p.Name == "start")?.Value;
        var endStr = parameters.FirstOrDefault(p => p.Name == "end")?.Value;

        if (string.IsNullOrEmpty(summary))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'summary' parameter is required." };
        if (string.IsNullOrEmpty(startStr))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'start' parameter is required." };
        if (string.IsNullOrEmpty(endStr))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'end' parameter is required." };

        if (!DateTimeOffset.TryParse(startStr, out var startTime))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'start' parameter must be a valid date/time." };
        if (!DateTimeOffset.TryParse(endStr, out var endTime))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'end' parameter must be a valid date/time." };

        var description = parameters.FirstOrDefault(p => p.Name == "description")?.Value;
        var attendeesStr = parameters.FirstOrDefault(p => p.Name == "attendees")?.Value;
        var calendarId = parameters.FirstOrDefault(p => p.Name == "calendarId")?.Value ?? "primary";

        var newEvent = new Event
        {
            Summary = summary,
            Description = description,
            Start = new EventDateTime { DateTimeDateTimeOffset = startTime },
            End = new EventDateTime { DateTimeDateTimeOffset = endTime }
        };

        if (!string.IsNullOrEmpty(attendeesStr))
        {
            var emails = attendeesStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            newEvent.Attendees = emails.Select(e => new EventAttendee { Email = e }).ToList();
        }

        var service = serviceFactory.CreateCalendarService(accessToken);

        var insertRequest = service.Events.Insert(newEvent, calendarId);
        var created = await insertRequest.ExecuteAsync();

        var result = new
        {
            id = created.Id,
            summary = created.Summary,
            start = created.Start?.DateTimeDateTimeOffset?.ToString("O") ?? "",
            end = created.End?.DateTimeDateTimeOffset?.ToString("O") ?? "",
            htmlLink = created.HtmlLink,
            status = "created"
        };

        var output = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });

        return new ExecuteResponse
        {
            Success = true,
            Output = output,
            OutputFormat = "json",
            OutputMessage = $"Created event: {summary}"
        };
    }
}

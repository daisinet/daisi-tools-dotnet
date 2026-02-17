using System.Text.Json;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Google.Tools;

/// <summary>
/// List calendar events from Google Calendar.
/// Supports filtering by time range, calendar ID, and result count.
/// </summary>
public class CalendarListTool : IGoogleToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(
        GoogleServiceFactory serviceFactory, string accessToken, List<ParameterValue> parameters)
    {
        var calendarId = parameters.FirstOrDefault(p => p.Name == "calendarId")?.Value ?? "primary";
        var timeMinStr = parameters.FirstOrDefault(p => p.Name == "timeMin")?.Value;
        var timeMaxStr = parameters.FirstOrDefault(p => p.Name == "timeMax")?.Value;
        var maxResultsStr = parameters.FirstOrDefault(p => p.Name == "maxResults")?.Value;

        var maxResults = 10;
        if (!string.IsNullOrEmpty(maxResultsStr) && int.TryParse(maxResultsStr, out var parsed))
            maxResults = Math.Clamp(parsed, 1, 100);

        var service = serviceFactory.CreateCalendarService(accessToken);

        var listRequest = service.Events.List(calendarId);
        listRequest.MaxResults = maxResults;
        listRequest.SingleEvents = true;
        listRequest.OrderBy = global::Google.Apis.Calendar.v3.EventsResource.ListRequest.OrderByEnum.StartTime;

        if (!string.IsNullOrEmpty(timeMinStr) && DateTimeOffset.TryParse(timeMinStr, out var timeMin))
            listRequest.TimeMinDateTimeOffset = timeMin;
        else
            listRequest.TimeMinDateTimeOffset = DateTimeOffset.UtcNow;

        if (!string.IsNullOrEmpty(timeMaxStr) && DateTimeOffset.TryParse(timeMaxStr, out var timeMax))
            listRequest.TimeMaxDateTimeOffset = timeMax;

        var events = await listRequest.ExecuteAsync();

        if (events.Items == null || events.Items.Count == 0)
        {
            return new ExecuteResponse
            {
                Success = true,
                Output = "[]",
                OutputFormat = "json",
                OutputMessage = "No events found."
            };
        }

        var results = events.Items.Select(e => new
        {
            id = e.Id,
            summary = e.Summary ?? "(no title)",
            start = e.Start?.DateTimeDateTimeOffset?.ToString("O") ?? e.Start?.Date ?? "",
            end = e.End?.DateTimeDateTimeOffset?.ToString("O") ?? e.End?.Date ?? "",
            description = e.Description ?? "",
            location = e.Location ?? ""
        }).ToList();

        var output = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });

        return new ExecuteResponse
        {
            Success = true,
            Output = output,
            OutputFormat = "json",
            OutputMessage = $"Found {results.Count} event(s)."
        };
    }
}

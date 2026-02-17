using System.Text.Json;
using Microsoft.Graph;
using SecureToolProvider.Common.Models;

namespace Daisi.SecureTools.Microsoft365.Tools;

/// <summary>
/// List calendar events from the user's primary calendar.
/// Supports optional timeMin/timeMax filtering via the $filter parameter.
/// Returns subject, start, end, location, and body preview.
/// </summary>
public class CalendarListTool : IGraphToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(GraphServiceClient graphClient, List<ParameterValue> parameters)
    {
        var timeMin = parameters.FirstOrDefault(p => p.Name == "timeMin")?.Value;
        var timeMax = parameters.FirstOrDefault(p => p.Name == "timeMax")?.Value;
        var maxResultsStr = parameters.FirstOrDefault(p => p.Name == "maxResults")?.Value;
        var maxResults = 10;
        if (!string.IsNullOrEmpty(maxResultsStr) && int.TryParse(maxResultsStr, out var parsed))
            maxResults = Math.Clamp(parsed, 1, 50);

        var events = await graphClient.Me.Calendar.Events.GetAsync(config =>
        {
            config.QueryParameters.Top = maxResults;
            config.QueryParameters.Select = ["subject", "start", "end", "location", "bodyPreview", "organizer"];
            config.QueryParameters.Orderby = ["start/dateTime"];

            // Build filter for time range if provided
            var filters = new List<string>();
            if (!string.IsNullOrEmpty(timeMin))
                filters.Add($"start/dateTime ge '{timeMin}'");
            if (!string.IsNullOrEmpty(timeMax))
                filters.Add($"end/dateTime le '{timeMax}'");

            if (filters.Count > 0)
                config.QueryParameters.Filter = string.Join(" and ", filters);
        });

        var results = (events?.Value ?? []).Select(e => new
        {
            id = e.Id,
            subject = e.Subject,
            start = e.Start?.DateTime,
            startTimeZone = e.Start?.TimeZone,
            end = e.End?.DateTime,
            endTimeZone = e.End?.TimeZone,
            location = e.Location?.DisplayName,
            organizer = e.Organizer?.EmailAddress?.Address,
            preview = e.BodyPreview
        });

        return new ExecuteResponse
        {
            Success = true,
            Output = JsonSerializer.Serialize(results),
            OutputFormat = "json",
            OutputMessage = $"Found {results.Count()} calendar events"
        };
    }
}

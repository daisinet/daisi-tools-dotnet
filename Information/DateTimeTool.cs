using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using System.Globalization;
using System.Text.Json;

namespace Daisi.Tools.Information
{
    public class DateTimeTool : DaisiToolBase
    {
        private const string P_ACTION = "action";
        private const string P_VALUE = "value";
        private const string P_VALUE2 = "value2";
        private const string P_FORMAT = "format";
        private const string P_TIMEZONE = "timezone";

        public override string Id => "daisi-info-datetime";
        public override string Name => "Daisi DateTime";

        public override string UseInstructions =>
            "Use this tool for date and time operations: getting the current date/time, formatting dates, " +
            "calculating date differences, or adding time periods. " +
            "This is the ONLY tool that knows the current date and time. " +
            "Keywords: date, time, now, today, current time, clock, date difference, add days, what time.";

        public override ToolParameter[] Parameters => [
            new ToolParameter() { Name = P_ACTION, Description = "The action: \"now\" (current date/time), \"format\" (format a date), \"parse\" (parse a date string), \"diff\" (difference between two dates), or \"add\" (add time to a date). Required.", IsRequired = true },
            new ToolParameter() { Name = P_VALUE, Description = "The date/time value to process. For 'now' this is optional. For 'add', use format like '2 days', '3 hours', '1 year'.", IsRequired = false },
            new ToolParameter() { Name = P_VALUE2, Description = "The second date/time value, used with 'diff' action.", IsRequired = false },
            new ToolParameter() { Name = P_FORMAT, Description = "The output format string (e.g. \"yyyy-MM-dd\", \"dddd, MMMM d, yyyy\"). Default is ISO 8601.", IsRequired = false },
            new ToolParameter() { Name = P_TIMEZONE, Description = "The timezone (e.g. \"UTC\", \"Eastern Standard Time\", \"Pacific Standard Time\"). Default is UTC.", IsRequired = false }
        ];

        public override ToolExecutionContext GetExecutionContext(IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
        {
            var action = parameters.GetParameterValueOrDefault(P_ACTION);
            var value = parameters.GetParameter(P_VALUE, false)?.Value;
            var value2 = parameters.GetParameter(P_VALUE2, false)?.Value;
            var format = parameters.GetParameter(P_FORMAT, false)?.Value;
            var timezone = parameters.GetParameterValueOrDefault(P_TIMEZONE, "UTC");

            return new ToolExecutionContext
            {
                ExecutionMessage = $"DateTime {action}",
                ExecutionTask = Task.Run(() => Execute(action, value, value2, format, timezone))
            };
        }

        internal static ToolResult Execute(string action, string? value, string? value2, string? format, string? timezone)
        {
            try
            {
                var tz = FindTimeZone(timezone ?? "UTC");
                var result = action?.ToLowerInvariant() switch
                {
                    "now" => ExecuteNow(format, tz),
                    "format" => ExecuteFormat(value!, format, tz),
                    "parse" => ExecuteParse(value!),
                    "diff" => ExecuteDiff(value!, value2!),
                    "add" => ExecuteAdd(value!, value2, format, tz),
                    _ => throw new ArgumentException($"Unknown action: {action}")
                };

                return new ToolResult
                {
                    Output = result,
                    OutputMessage = $"DateTime {action} result",
                    OutputFormat = Protos.V1.InferenceOutputFormats.PlainText,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new ToolResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        private static string ExecuteNow(string? format, TimeZoneInfo tz)
        {
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            return format is not null ? now.ToString(format) : now.ToString("o");
        }

        private static string ExecuteFormat(string value, string? format, TimeZoneInfo tz)
        {
            var dt = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            if (dt.Kind == DateTimeKind.Utc)
                dt = TimeZoneInfo.ConvertTimeFromUtc(dt, tz);
            return format is not null ? dt.ToString(format) : dt.ToString("o");
        }

        private static string ExecuteParse(string value)
        {
            var dt = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            return JsonSerializer.Serialize(new
            {
                iso = dt.ToString("o"),
                year = dt.Year,
                month = dt.Month,
                day = dt.Day,
                hour = dt.Hour,
                minute = dt.Minute,
                second = dt.Second,
                dayOfWeek = dt.DayOfWeek.ToString()
            });
        }

        private static string ExecuteDiff(string value1, string value2)
        {
            var dt1 = DateTime.Parse(value1, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            var dt2 = DateTime.Parse(value2, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            var diff = dt2 - dt1;

            return JsonSerializer.Serialize(new
            {
                totalDays = diff.TotalDays,
                totalHours = diff.TotalHours,
                totalMinutes = diff.TotalMinutes,
                totalSeconds = diff.TotalSeconds,
                days = diff.Days,
                hours = diff.Hours,
                minutes = diff.Minutes,
                seconds = diff.Seconds
            });
        }

        private static string ExecuteAdd(string dateValue, string? amount, string? format, TimeZoneInfo tz)
        {
            var dt = DateTime.Parse(dateValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            if (amount is not null)
                dt = AddDuration(dt, amount);
            if (dt.Kind == DateTimeKind.Utc)
                dt = TimeZoneInfo.ConvertTimeFromUtc(dt, tz);
            return format is not null ? dt.ToString(format) : dt.ToString("o");
        }

        internal static DateTime AddDuration(DateTime dt, string duration)
        {
            var parts = duration.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || !double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
                throw new ArgumentException($"Invalid duration format: '{duration}'. Use format like '2 days', '3 hours'.");

            var unit = parts[1].ToLowerInvariant().TrimEnd('s');
            return unit switch
            {
                "second" => dt.AddSeconds(amount),
                "minute" => dt.AddMinutes(amount),
                "hour" => dt.AddHours(amount),
                "day" => dt.AddDays(amount),
                "week" => dt.AddDays(amount * 7),
                "month" => dt.AddMonths((int)amount),
                "year" => dt.AddYears((int)amount),
                _ => throw new ArgumentException($"Unknown time unit: '{parts[1]}'")
            };
        }

        private static TimeZoneInfo FindTimeZone(string timezone)
        {
            if (string.Equals(timezone, "UTC", StringComparison.OrdinalIgnoreCase))
                return TimeZoneInfo.Utc;

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timezone);
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }
    }
}

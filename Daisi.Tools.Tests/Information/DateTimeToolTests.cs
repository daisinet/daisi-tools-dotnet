using Daisi.SDK.Models.Tools;
using Daisi.Tools.Information;
using Daisi.Tools.Tests.Helpers;
using System.Text.Json;

namespace Daisi.Tools.Tests.Information
{
    public class DateTimeToolTests
    {
        [Fact]
        public void Id_ReturnsExpectedValue()
        {
            var tool = new DateTimeTool();
            Assert.Equal("daisi-info-datetime", tool.Id);
        }

        [Fact]
        public void Parameters_ActionIsRequired()
        {
            var tool = new DateTimeTool();
            Assert.True(tool.Parameters.First(p => p.Name == "action").IsRequired);
        }

        [Fact]
        public void Parameters_ValueIsOptional()
        {
            var tool = new DateTimeTool();
            Assert.False(tool.Parameters.First(p => p.Name == "value").IsRequired);
        }

        [Fact]
        public void Execute_Now_ReturnsCurrentDateTime()
        {
            var result = DateTimeTool.Execute("now", null, null, null, "UTC");
            Assert.True(result.Success);
            Assert.NotNull(result.Output);
            // Should be parseable as a date
            Assert.True(DateTime.TryParse(result.Output, out _));
        }

        [Fact]
        public void Execute_Now_WithFormat_ReturnsFormattedDate()
        {
            var result = DateTimeTool.Execute("now", null, null, "yyyy-MM-dd", "UTC");
            Assert.True(result.Success);
            Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", result.Output);
        }

        [Fact]
        public void Execute_Format_FormatsDateCorrectly()
        {
            var result = DateTimeTool.Execute("format", "2025-06-15T10:30:00Z", null, "MMMM d, yyyy", "UTC");
            Assert.True(result.Success);
            Assert.Equal("June 15, 2025", result.Output);
        }

        [Fact]
        public void Execute_Parse_ReturnsDateComponents()
        {
            var result = DateTimeTool.Execute("parse", "2025-03-15T14:30:00", null, null, "UTC");
            Assert.True(result.Success);

            using var doc = JsonDocument.Parse(result.Output);
            Assert.Equal(2025, doc.RootElement.GetProperty("year").GetInt32());
            Assert.Equal(3, doc.RootElement.GetProperty("month").GetInt32());
            Assert.Equal(15, doc.RootElement.GetProperty("day").GetInt32());
        }

        [Fact]
        public void Execute_Diff_ReturnsTimeDifference()
        {
            var result = DateTimeTool.Execute("diff", "2025-01-01", "2025-01-11", null, "UTC");
            Assert.True(result.Success);

            using var doc = JsonDocument.Parse(result.Output);
            Assert.Equal(10, doc.RootElement.GetProperty("totalDays").GetDouble());
        }

        [Fact]
        public void Execute_Add_AddsDaysCorrectly()
        {
            var result = DateTimeTool.Execute("add", "2025-01-01T00:00:00Z", "5 days", "yyyy-MM-dd", "UTC");
            Assert.True(result.Success);
            Assert.Equal("2025-01-06", result.Output);
        }

        [Fact]
        public void AddDuration_VariousUnits()
        {
            var dt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            Assert.Equal(new DateTime(2025, 1, 1, 0, 0, 30), DateTimeTool.AddDuration(dt, "30 seconds"));
            Assert.Equal(new DateTime(2025, 1, 1, 2, 0, 0), DateTimeTool.AddDuration(dt, "2 hours"));
            Assert.Equal(new DateTime(2025, 1, 8, 0, 0, 0), DateTimeTool.AddDuration(dt, "1 week"));
            Assert.Equal(new DateTime(2025, 4, 1, 0, 0, 0), DateTimeTool.AddDuration(dt, "3 months"));
            Assert.Equal(new DateTime(2027, 1, 1, 0, 0, 0), DateTimeTool.AddDuration(dt, "2 years"));
        }

        [Fact]
        public void Execute_UnknownAction_ReturnsError()
        {
            var result = DateTimeTool.Execute("unknown", null, null, null, "UTC");
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Execute_ViaContext_Works()
        {
            var tool = new DateTimeTool();
            var context = new MockToolContext();
            var parameters = new ToolParameterBase[]
            {
                new() { Name = "action", Value = "now", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
        }
    }
}

using Daisi.Protos.V1;
using Daisi.SDK.Models.Tools;
using Daisi.Tools.Files;
using Daisi.Tools.Tests.Helpers;

namespace Daisi.Tools.Tests.Files
{
    public class ReadFileToolTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly MockToolContext _context;

        public ReadFileToolTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"daisi-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
            _context = new MockToolContext();
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [Fact]
        public void Id_ReturnsExpectedValue()
        {
            var tool = new ReadFileTool();
            Assert.Equal("daisi-files-read", tool.Id);
        }

        [Fact]
        public async Task Execute_ReadsFileSuccessfully()
        {
            var tool = new ReadFileTool();
            var filePath = Path.Combine(_tempDir, "test.txt");
            await File.WriteAllTextAsync(filePath, "Hello, World!");

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "path", Value = filePath, IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(_context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            Assert.Equal("Hello, World!", result.Output);
            Assert.Equal(InferenceOutputFormats.PlainText, result.OutputFormat);
        }

        [Fact]
        public async Task Execute_FileNotFound_ReturnsError()
        {
            var tool = new ReadFileTool();
            var filePath = Path.Combine(_tempDir, "nonexistent.txt");

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "path", Value = filePath, IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(_context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.False(result.Success);
            Assert.Contains("File not found", result.ErrorMessage);
        }

        [Fact]
        public async Task Execute_LargeFile_Truncates()
        {
            var tool = new ReadFileTool();
            var filePath = Path.Combine(_tempDir, "large.txt");

            // Create a file larger than 1MB
            var content = new string('A', 1_100_000);
            await File.WriteAllTextAsync(filePath, content);

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "path", Value = filePath, IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(_context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            Assert.Equal(1_048_576, result.Output.Length);
            Assert.Contains("truncated", result.OutputMessage);
        }

        [Fact]
        public async Task Execute_SmallFile_NoTruncation()
        {
            var tool = new ReadFileTool();
            var filePath = Path.Combine(_tempDir, "small.txt");
            await File.WriteAllTextAsync(filePath, "Small content");

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "path", Value = filePath, IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(_context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            Assert.DoesNotContain("truncated", result.OutputMessage);
        }
    }
}

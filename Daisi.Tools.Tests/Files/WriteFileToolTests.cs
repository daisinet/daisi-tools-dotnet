using Daisi.Protos.V1;
using Daisi.SDK.Models.Tools;
using Daisi.Tools.Files;
using Daisi.Tools.Tests.Helpers;

namespace Daisi.Tools.Tests.Files
{
    public class WriteFileToolTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly MockToolContext _context;

        public WriteFileToolTests()
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
            var tool = new WriteFileTool();
            Assert.Equal("daisi-files-write", tool.Id);
        }

        [Fact]
        public async Task Execute_WritesFileSuccessfully()
        {
            var tool = new WriteFileTool();
            var filePath = Path.Combine(_tempDir, "output.txt");

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "path", Value = filePath, IsRequired = true },
                new() { Name = "content", Value = "Hello, World!", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(_context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            Assert.True(File.Exists(filePath));
            Assert.Equal("Hello, World!", await File.ReadAllTextAsync(filePath));
            Assert.Contains("bytes", result.Output);
        }

        [Fact]
        public async Task Execute_CreatesParentDirectories()
        {
            var tool = new WriteFileTool();
            var filePath = Path.Combine(_tempDir, "sub", "dir", "file.txt");

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "path", Value = filePath, IsRequired = true },
                new() { Name = "content", Value = "Nested content", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(_context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            Assert.True(File.Exists(filePath));
            Assert.Equal("Nested content", await File.ReadAllTextAsync(filePath));
        }

        [Fact]
        public async Task Execute_OverwritesExistingFile()
        {
            var tool = new WriteFileTool();
            var filePath = Path.Combine(_tempDir, "overwrite.txt");

            await File.WriteAllTextAsync(filePath, "Original content");

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "path", Value = filePath, IsRequired = true },
                new() { Name = "content", Value = "New content", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(_context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            Assert.Equal("New content", await File.ReadAllTextAsync(filePath));
        }

        [Fact]
        public async Task Execute_OutputFormatIsPlainText()
        {
            var tool = new WriteFileTool();
            var filePath = Path.Combine(_tempDir, "format.txt");

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "path", Value = filePath, IsRequired = true },
                new() { Name = "content", Value = "Test", IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(_context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.Equal(InferenceOutputFormats.PlainText, result.OutputFormat);
        }
    }
}

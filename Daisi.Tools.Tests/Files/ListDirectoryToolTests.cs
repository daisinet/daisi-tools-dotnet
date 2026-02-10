using Daisi.Protos.V1;
using Daisi.SDK.Models.Tools;
using Daisi.Tools.Files;
using Daisi.Tools.Tests.Helpers;
using System.Text.Json;

namespace Daisi.Tools.Tests.Files
{
    public class ListDirectoryToolTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly MockToolContext _context;

        public ListDirectoryToolTests()
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
            var tool = new ListDirectoryTool();
            Assert.Equal("daisi-files-list-directory", tool.Id);
        }

        [Fact]
        public async Task Execute_ListsFilesAndDirectories()
        {
            var tool = new ListDirectoryTool();

            // Create test files and subdirectory
            await File.WriteAllTextAsync(Path.Combine(_tempDir, "file1.txt"), "content1");
            await File.WriteAllTextAsync(Path.Combine(_tempDir, "file2.txt"), "content2");
            Directory.CreateDirectory(Path.Combine(_tempDir, "subdir"));

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "path", Value = _tempDir, IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(_context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            Assert.Equal(InferenceOutputFormats.Json, result.OutputFormat);

            var entries = JsonSerializer.Deserialize<JsonElement[]>(result.Output);
            Assert.NotNull(entries);
            Assert.Equal(3, entries.Length);

            // Check that we have both files and directory
            Assert.Contains(entries, e => e.GetProperty("name").GetString() == "subdir" && e.GetProperty("type").GetString() == "directory");
            Assert.Contains(entries, e => e.GetProperty("name").GetString() == "file1.txt" && e.GetProperty("type").GetString() == "file");
            Assert.Contains(entries, e => e.GetProperty("name").GetString() == "file2.txt" && e.GetProperty("type").GetString() == "file");
        }

        [Fact]
        public async Task Execute_PatternFilter_FiltersResults()
        {
            var tool = new ListDirectoryTool();

            await File.WriteAllTextAsync(Path.Combine(_tempDir, "file1.txt"), "content");
            await File.WriteAllTextAsync(Path.Combine(_tempDir, "file2.cs"), "content");
            await File.WriteAllTextAsync(Path.Combine(_tempDir, "file3.txt"), "content");

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "path", Value = _tempDir, IsRequired = true },
                new() { Name = "pattern", Value = "*.txt", IsRequired = false }
            };

            var execContext = tool.GetExecutionContext(_context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);

            var entries = JsonSerializer.Deserialize<JsonElement[]>(result.Output);
            Assert.NotNull(entries);
            Assert.Equal(2, entries.Length);
            Assert.All(entries, e => Assert.EndsWith(".txt", e.GetProperty("name").GetString()));
        }

        [Fact]
        public async Task Execute_DirectoryNotFound_ReturnsError()
        {
            var tool = new ListDirectoryTool();
            var nonExistentPath = Path.Combine(_tempDir, "nonexistent");

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "path", Value = nonExistentPath, IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(_context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.False(result.Success);
            Assert.Contains("Directory not found", result.ErrorMessage);
        }

        [Fact]
        public async Task Execute_EmptyDirectory_ReturnsEmptyArray()
        {
            var tool = new ListDirectoryTool();

            var parameters = new ToolParameterBase[]
            {
                new() { Name = "path", Value = _tempDir, IsRequired = true }
            };

            var execContext = tool.GetExecutionContext(_context, CancellationToken.None, parameters);
            var result = await execContext.ExecutionTask;

            Assert.True(result.Success);
            Assert.Equal("[]", result.Output);
        }
    }
}

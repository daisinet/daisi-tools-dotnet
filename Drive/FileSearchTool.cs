using Daisi.Protos.V1;
using Daisi.SDK.Clients.V1.Orc;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Daisi.Tools.Drive
{
    public class FileSearchTool : DaisiToolBase
    {
        private const string P_QUERY = "query";
        private const string P_TOP_K = "top-k";
        private const string P_REPOSITORY_ID = "repository-id";

        public override string Id => "daisi-drive-search";
        public override string Name => "Daisi Drive Search";

        public override string UseInstructions =>
            "Use this tool to search Drive files semantically. Finds files whose content is relevant to the query. " +
            "Keywords: search files, find in drive, search documents, find document, search my files. " +
            "Use when the user references files with #filename or asks to find content across their files. " +
            "Optionally scope to a specific repository using repository-id.";

        public override ToolParameter[] Parameters => [
            new ToolParameter(){
                Name = P_QUERY,
                Description = "The search query — can be a semantic description of what to find.",
                IsRequired = true
            },
            new ToolParameter(){
                Name = P_TOP_K,
                Description = "Maximum number of results to return. Default is 5.",
                IsRequired = false
            },
            new ToolParameter(){
                Name = P_REPOSITORY_ID,
                Description = "Optional repository ID to scope search to a specific repository.",
                IsRequired = false
            }
        ];

        public override ToolExecutionContext GetExecutionContext(IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
        {
            var query = parameters.GetParameter(P_QUERY).Value;
            var topKStr = parameters.GetParameterValueOrDefault(P_TOP_K, "5");
            if (!int.TryParse(topKStr, out var topK))
                topK = 5;
            var repositoryId = parameters.GetParameterValueOrDefault(P_REPOSITORY_ID, null);

            Task<ToolResult> task = SearchFiles(toolContext, query, topK, repositoryId, cancellation);

            return new ToolExecutionContext()
            {
                ExecutionTask = task,
                ExecutionMessage = $"Searching Drive files: {query}"
            };
        }

        private async Task<ToolResult> SearchFiles(IToolContext toolContext, string query, int topK, string? repositoryId, CancellationToken cancellation)
        {
            try
            {
                var driveClientFactory = toolContext.Services.GetService<DriveClientFactory>();
                if (driveClientFactory is null)
                {
                    return new ToolResult()
                    {
                        Success = false,
                        ErrorMessage = "Drive client is not available."
                    };
                }

                var client = driveClientFactory.Create();
                var request = new VectorSearchRequest
                {
                    Query = query,
                    TopK = topK,
                    IncludeSystemFiles = true
                };

                if (!string.IsNullOrEmpty(repositoryId))
                    request.RepositoryIds.Add(repositoryId);

                var response = await client.VectorSearchAsync(request, cancellationToken: cancellation);

                var results = response.Results.Select(r => new
                {
                    r.FileId,
                    r.FileName,
                    r.Snippet,
                    r.Score,
                    r.ChunkIndex
                }).ToList();

                return new ToolResult()
                {
                    Success = true,
                    Output = JsonSerializer.Serialize(results),
                    OutputFormat = InferenceOutputFormats.Json,
                    OutputMessage = $"Found {results.Count} matching file sections"
                };
            }
            catch (Exception ex)
            {
                return new ToolResult() { Success = false, ErrorMessage = ex.Message };
            }
        }
    }
}

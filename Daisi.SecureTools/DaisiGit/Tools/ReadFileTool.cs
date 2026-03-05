using Daisi.SecureTools.Provider.Common.Models;

namespace Daisi.SecureTools.DaisiGit.Tools;

/// <summary>
/// Read the content of a single file in a repository.
/// </summary>
public class ReadFileTool(DaisiGitClient client) : IToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(string baseUrl, string sessionId, List<ParameterValue> parameters)
    {
        var owner = parameters.FirstOrDefault(p => p.Name == "owner")?.Value;
        var repo = parameters.FirstOrDefault(p => p.Name == "repo")?.Value;
        var branch = parameters.FirstOrDefault(p => p.Name == "branch")?.Value ?? "main";
        var path = parameters.FirstOrDefault(p => p.Name == "path")?.Value;

        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo) || string.IsNullOrEmpty(path))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'owner', 'repo', and 'path' parameters are required." };

        var apiPath = $"/api/git/repos/{owner}/{repo}/blob/{branch}/{path}";
        var result = await client.GetAsync(baseUrl, sessionId, apiPath);

        return new ExecuteResponse
        {
            Success = true,
            Output = result.ToString(),
            OutputFormat = "markdown",
            OutputMessage = $"Read {path} from {owner}/{repo}"
        };
    }
}

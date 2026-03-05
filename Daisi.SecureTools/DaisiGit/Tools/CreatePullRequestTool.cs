using Daisi.SecureTools.Provider.Common.Models;

namespace Daisi.SecureTools.DaisiGit.Tools;

/// <summary>
/// Create a new pull request in a repository.
/// </summary>
public class CreatePullRequestTool(DaisiGitClient client) : IToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(string baseUrl, string sessionId, List<ParameterValue> parameters)
    {
        var owner = parameters.FirstOrDefault(p => p.Name == "owner")?.Value;
        var repo = parameters.FirstOrDefault(p => p.Name == "repo")?.Value;
        var title = parameters.FirstOrDefault(p => p.Name == "title")?.Value;
        var description = parameters.FirstOrDefault(p => p.Name == "description")?.Value;
        var sourceBranch = parameters.FirstOrDefault(p => p.Name == "sourceBranch")?.Value;
        var targetBranch = parameters.FirstOrDefault(p => p.Name == "targetBranch")?.Value ?? "main";

        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo) ||
            string.IsNullOrEmpty(title) || string.IsNullOrEmpty(sourceBranch))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'owner', 'repo', 'title', and 'sourceBranch' parameters are required." };

        var apiPath = $"/api/git/repos/{owner}/{repo}/pulls";
        var result = await client.PostAsync(baseUrl, sessionId, apiPath, new
        {
            title,
            description,
            sourceBranch,
            targetBranch
        });

        return new ExecuteResponse
        {
            Success = true,
            Output = result.ToString(),
            OutputFormat = "markdown",
            OutputMessage = $"Created PR \"{title}\" in {owner}/{repo} ({sourceBranch} → {targetBranch})"
        };
    }
}

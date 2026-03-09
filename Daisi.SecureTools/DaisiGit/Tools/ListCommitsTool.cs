using Daisi.SecureTools.Provider.Common.Models;

namespace Daisi.SecureTools.DaisiGit.Tools;

/// <summary>
/// List commits on a branch in a repository.
/// </summary>
public class ListCommitsTool(DaisiGitClient client) : IToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(string baseUrl, string sessionId, List<ParameterValue> parameters)
    {
        var owner = parameters.FirstOrDefault(p => p.Name == "owner")?.Value;
        var repo = parameters.FirstOrDefault(p => p.Name == "repo")?.Value;
        var branch = parameters.FirstOrDefault(p => p.Name == "branch")?.Value ?? "main";
        var take = parameters.FirstOrDefault(p => p.Name == "maxResults")?.Value ?? "20";

        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'owner' and 'repo' parameters are required." };

        var apiPath = $"/api/git/repos/{owner}/{repo}/commits/{branch}?take={take}";
        var result = await client.GetAsync(baseUrl, sessionId, apiPath);

        return new ExecuteResponse
        {
            Success = true,
            Output = result.ToString(),
            OutputFormat = "markdown",
            OutputMessage = $"Listed commits on {branch} in {owner}/{repo}"
        };
    }
}

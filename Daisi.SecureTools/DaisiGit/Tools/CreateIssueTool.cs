using Daisi.SecureTools.Provider.Common.Models;

namespace Daisi.SecureTools.DaisiGit.Tools;

/// <summary>
/// Create a new issue in a repository.
/// </summary>
public class CreateIssueTool(DaisiGitClient client) : IToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(string baseUrl, string sessionId, List<ParameterValue> parameters)
    {
        var owner = parameters.FirstOrDefault(p => p.Name == "owner")?.Value;
        var repo = parameters.FirstOrDefault(p => p.Name == "repo")?.Value;
        var title = parameters.FirstOrDefault(p => p.Name == "title")?.Value;
        var description = parameters.FirstOrDefault(p => p.Name == "description")?.Value;

        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo) || string.IsNullOrEmpty(title))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'owner', 'repo', and 'title' parameters are required." };

        var apiPath = $"/api/git/repos/{owner}/{repo}/issues";
        var result = await client.PostAsync(baseUrl, sessionId, apiPath, new { title, description });

        return new ExecuteResponse
        {
            Success = true,
            Output = result.ToString(),
            OutputFormat = "markdown",
            OutputMessage = $"Created issue \"{title}\" in {owner}/{repo}"
        };
    }
}

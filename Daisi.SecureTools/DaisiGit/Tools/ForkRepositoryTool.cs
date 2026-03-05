using Daisi.SecureTools.Provider.Common.Models;

namespace Daisi.SecureTools.DaisiGit.Tools;

public class ForkRepositoryTool(DaisiGitClient client) : IToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(string baseUrl, string sessionId, List<ParameterValue> parameters)
    {
        var owner = parameters.FirstOrDefault(p => p.Name == "owner")?.Value;
        var repo = parameters.FirstOrDefault(p => p.Name == "repo")?.Value;

        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'owner' and 'repo' parameters are required." };

        var result = await client.PostAsync(baseUrl, sessionId, $"/api/git/repos/{owner}/{repo}/forks", new { });
        return new ExecuteResponse { Success = true, Output = result.ToString(), OutputFormat = "markdown", OutputMessage = $"Forked {owner}/{repo}" };
    }
}

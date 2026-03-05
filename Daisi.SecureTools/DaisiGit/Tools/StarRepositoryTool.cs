using Daisi.SecureTools.Provider.Common.Models;

namespace Daisi.SecureTools.DaisiGit.Tools;

public class StarRepositoryTool(DaisiGitClient client) : IToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(string baseUrl, string sessionId, List<ParameterValue> parameters)
    {
        var owner = parameters.FirstOrDefault(p => p.Name == "owner")?.Value;
        var repo = parameters.FirstOrDefault(p => p.Name == "repo")?.Value;
        var action = parameters.FirstOrDefault(p => p.Name == "action")?.Value ?? "star";

        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'owner' and 'repo' parameters are required." };

        if (action.Equals("unstar", StringComparison.OrdinalIgnoreCase))
        {
            await client.DeleteAsync(baseUrl, sessionId, $"/api/git/repos/{owner}/{repo}/star");
            return new ExecuteResponse { Success = true, Output = "Unstarred", OutputFormat = "markdown", OutputMessage = $"Unstarred {owner}/{repo}" };
        }

        await client.PutAsync(baseUrl, sessionId, $"/api/git/repos/{owner}/{repo}/star");
        return new ExecuteResponse { Success = true, Output = "Starred", OutputFormat = "markdown", OutputMessage = $"Starred {owner}/{repo}" };
    }
}

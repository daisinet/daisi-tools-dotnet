using Daisi.SecureTools.Provider.Common.Models;

namespace Daisi.SecureTools.DaisiGit.Tools;

/// <summary>
/// List issues in a repository, optionally filtered by status.
/// </summary>
public class ListIssuesTool(DaisiGitClient client) : IToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(string baseUrl, string sessionId, List<ParameterValue> parameters)
    {
        var owner = parameters.FirstOrDefault(p => p.Name == "owner")?.Value;
        var repo = parameters.FirstOrDefault(p => p.Name == "repo")?.Value;
        var status = parameters.FirstOrDefault(p => p.Name == "status")?.Value;

        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'owner' and 'repo' parameters are required." };

        var apiPath = $"/api/git/repos/{owner}/{repo}/issues";
        if (!string.IsNullOrEmpty(status))
            apiPath += $"?status={status}";

        var result = await client.GetAsync(baseUrl, sessionId, apiPath);

        return new ExecuteResponse
        {
            Success = true,
            Output = result.ToString(),
            OutputFormat = "markdown",
            OutputMessage = $"Listed issues in {owner}/{repo}"
        };
    }
}

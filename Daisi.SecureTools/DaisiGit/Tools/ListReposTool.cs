using Daisi.SecureTools.Provider.Common.Models;

namespace Daisi.SecureTools.DaisiGit.Tools;

/// <summary>
/// Lists repositories accessible to the current user.
/// </summary>
public class ListReposTool(DaisiGitClient client) : IToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(string baseUrl, string sessionId, List<ParameterValue> parameters)
    {
        var owner = parameters.FirstOrDefault(p => p.Name == "owner")?.Value;
        var path = string.IsNullOrEmpty(owner) ? "/api/git/repos" : $"/api/git/repos?owner={owner}";

        var result = await client.GetAsync(baseUrl, sessionId, path);

        return new ExecuteResponse
        {
            Success = true,
            Output = result.ToString(),
            OutputFormat = "markdown",
            OutputMessage = "Listed repositories"
        };
    }
}

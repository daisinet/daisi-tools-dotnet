using Daisi.SecureTools.Provider.Common.Models;

namespace Daisi.SecureTools.DaisiGit.Tools;

/// <summary>
/// Add a comment to an issue or pull request.
/// </summary>
public class AddCommentTool(DaisiGitClient client) : IToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(string baseUrl, string sessionId, List<ParameterValue> parameters)
    {
        var owner = parameters.FirstOrDefault(p => p.Name == "owner")?.Value;
        var repo = parameters.FirstOrDefault(p => p.Name == "repo")?.Value;
        var type = parameters.FirstOrDefault(p => p.Name == "type")?.Value ?? "issues";
        var numberStr = parameters.FirstOrDefault(p => p.Name == "number")?.Value;
        var body = parameters.FirstOrDefault(p => p.Name == "body")?.Value;

        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo) ||
            string.IsNullOrEmpty(numberStr) || string.IsNullOrEmpty(body))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'owner', 'repo', 'number', and 'body' parameters are required." };

        var endpoint = type.ToLowerInvariant() == "pulls" ? "pulls" : "issues";
        var apiPath = $"/api/git/repos/{owner}/{repo}/{endpoint}/{numberStr}/comments";
        var result = await client.PostAsync(baseUrl, sessionId, apiPath, new { body });

        return new ExecuteResponse
        {
            Success = true,
            Output = result.ToString(),
            OutputFormat = "markdown",
            OutputMessage = $"Added comment to {endpoint} #{numberStr} in {owner}/{repo}"
        };
    }
}

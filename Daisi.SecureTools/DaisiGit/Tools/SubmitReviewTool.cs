using Daisi.SecureTools.Provider.Common.Models;

namespace Daisi.SecureTools.DaisiGit.Tools;

/// <summary>
/// Submit a review on a pull request.
/// </summary>
public class SubmitReviewTool(DaisiGitClient client) : IToolExecutor
{
    public async Task<ExecuteResponse> ExecuteAsync(string baseUrl, string sessionId, List<ParameterValue> parameters)
    {
        var owner = parameters.FirstOrDefault(p => p.Name == "owner")?.Value;
        var repo = parameters.FirstOrDefault(p => p.Name == "repo")?.Value;
        var prNumberStr = parameters.FirstOrDefault(p => p.Name == "prNumber")?.Value;
        var state = parameters.FirstOrDefault(p => p.Name == "state")?.Value;
        var body = parameters.FirstOrDefault(p => p.Name == "body")?.Value;

        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo) || string.IsNullOrEmpty(prNumberStr))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'owner', 'repo', and 'prNumber' parameters are required." };

        if (!int.TryParse(prNumberStr, out var prNumber))
            return new ExecuteResponse { Success = false, ErrorMessage = "The 'prNumber' parameter must be a valid integer." };

        var apiPath = $"/api/git/repos/{owner}/{repo}/pulls/{prNumber}/reviews";
        var result = await client.PostAsync(baseUrl, sessionId, apiPath, new { state, body });

        return new ExecuteResponse
        {
            Success = true,
            Output = result.ToString(),
            OutputFormat = "markdown",
            OutputMessage = $"Submitted {state ?? "comment"} review on PR #{prNumber} in {owner}/{repo}"
        };
    }
}

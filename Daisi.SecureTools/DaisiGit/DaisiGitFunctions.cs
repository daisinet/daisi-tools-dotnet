using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Daisi.SecureTools.Provider.Common;
using Daisi.SecureTools.Provider.Common.Models;
using Daisi.SecureTools.DaisiGit.Tools;

namespace Daisi.SecureTools.DaisiGit;

/// <summary>
/// Azure Functions endpoints for the DaisiGit secure tool provider.
/// Provides repository browsing, issue tracking, and pull request management
/// tools for AI agents on the Daisinet platform.
/// </summary>
public class DaisiGitFunctions : SecureToolFunctionBase
{
    private readonly DaisiGitClient _gitClient;

    private static readonly Dictionary<string, Func<DaisiGitClient, IToolExecutor>> ToolMap = new()
    {
        ["daisi-daisigit-list-repos"] = c => new ListReposTool(c),
        ["daisi-daisigit-browse-files"] = c => new BrowseFilesTool(c),
        ["daisi-daisigit-read-file"] = c => new ReadFileTool(c),
        ["daisi-daisigit-list-commits"] = c => new ListCommitsTool(c),
        ["daisi-daisigit-list-issues"] = c => new ListIssuesTool(c),
        ["daisi-daisigit-create-issue"] = c => new CreateIssueTool(c),
        ["daisi-daisigit-list-pulls"] = c => new ListPullRequestsTool(c),
        ["daisi-daisigit-create-pull"] = c => new CreatePullRequestTool(c),
        ["daisi-daisigit-add-comment"] = c => new AddCommentTool(c),
        ["daisi-daisigit-list-reviews"] = c => new ListReviewsTool(c),
        ["daisi-daisigit-submit-review"] = c => new SubmitReviewTool(c),
    };

    public DaisiGitFunctions(
        ISetupStore setupStore,
        AuthValidator authValidator,
        DaisiGitClient gitClient,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<DaisiGitFunctions> logger)
        : base(setupStore, authValidator, logger, httpClientFactory, configuration)
    {
        _gitClient = gitClient;
    }

    [Function("daisigit-install")]
    public Task<HttpResponseData> Install(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "daisigit/install")] HttpRequestData req)
        => HandleInstallAsync(req);

    [Function("daisigit-uninstall")]
    public Task<HttpResponseData> Uninstall(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "daisigit/uninstall")] HttpRequestData req)
        => HandleUninstallAsync(req);

    [Function("daisigit-configure")]
    public Task<HttpResponseData> Configure(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "daisigit/configure")] HttpRequestData req)
        => HandleConfigureAsync(req);

    [Function("daisigit-configure-status")]
    public Task<HttpResponseData> ConfigureStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "daisigit/configure/status")] HttpRequestData req)
        => HandleConfigureStatusAsync(req);

    [Function("daisigit-execute")]
    public Task<HttpResponseData> Execute(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "daisigit/execute")] HttpRequestData req)
        => HandleExecuteAsync(req);

    protected override async Task<ExecuteResponse> ExecuteToolAsync(
        string installId, string toolId, List<ParameterValue> parameters, Dictionary<string, string> setup)
    {
        if (!ToolMap.TryGetValue(toolId, out var toolFactory))
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = $"Unknown tool: {toolId}"
            };
        }

        var baseUrl = setup.GetValueOrDefault("serverUrl");
        if (string.IsNullOrEmpty(baseUrl))
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = "DaisiGit server URL is not configured. Please configure the tool first."
            };
        }

        var sessionId = setup.GetValueOrDefault("sessionId", "");
        var tool = toolFactory(_gitClient);

        try
        {
            return await tool.ExecuteAsync(baseUrl, sessionId, parameters);
        }
        catch (HttpRequestException ex)
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = $"DaisiGit API error: {ex.Message}"
            };
        }
    }
}

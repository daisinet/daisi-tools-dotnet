using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureToolProvider.Common;
using SecureToolProvider.Common.Models;
using Daisi.SecureTools.Firecrawl.Tools;

namespace Daisi.SecureTools.Firecrawl;

/// <summary>
/// Azure Functions endpoints for the Firecrawl secure tool provider.
/// Supports scraping, crawling, searching, extracting, and mapping web content
/// via the Firecrawl API.
/// </summary>
public class FirecrawlFunctions : SecureToolFunctionBase
{
    private readonly FirecrawlClient _firecrawlClient;

    private static readonly Dictionary<string, Func<FirecrawlClient, IToolExecutor>> ToolMap = new()
    {
        ["daisi-firecrawl-scrape"] = c => new ScrapeTool(c),
        ["daisi-firecrawl-crawl"] = c => new CrawlTool(c),
        ["daisi-firecrawl-search"] = c => new SearchTool(c),
        ["daisi-firecrawl-extract"] = c => new ExtractTool(c),
        ["daisi-firecrawl-map"] = c => new MapTool(c),
    };

    public FirecrawlFunctions(
        ISetupStore setupStore,
        AuthValidator authValidator,
        FirecrawlClient firecrawlClient,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<FirecrawlFunctions> logger)
        : base(setupStore, authValidator, logger, httpClientFactory, configuration)
    {
        _firecrawlClient = firecrawlClient;
    }

    [Function("firecrawl-install")]
    public Task<HttpResponseData> Install(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "firecrawl/install")] HttpRequestData req)
        => HandleInstallAsync(req);

    [Function("firecrawl-uninstall")]
    public Task<HttpResponseData> Uninstall(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "firecrawl/uninstall")] HttpRequestData req)
        => HandleUninstallAsync(req);

    [Function("firecrawl-configure")]
    public Task<HttpResponseData> Configure(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "firecrawl/configure")] HttpRequestData req)
        => HandleConfigureAsync(req);

    [Function("firecrawl-configure-status")]
    public Task<HttpResponseData> ConfigureStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "firecrawl/configure/status")] HttpRequestData req)
        => HandleConfigureStatusAsync(req);

    [Function("firecrawl-execute")]
    public Task<HttpResponseData> Execute(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "firecrawl/execute")] HttpRequestData req)
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

        var apiKey = setup.GetValueOrDefault("apiKey");
        if (string.IsNullOrEmpty(apiKey))
        {
            return new ExecuteResponse
            {
                Success = false,
                ErrorMessage = "Firecrawl API key is not configured. Please configure the tool first."
            };
        }

        var baseUrl = setup.GetValueOrDefault("baseUrl", "https://api.firecrawl.dev");
        var tool = toolFactory(_firecrawlClient);
        return await tool.ExecuteAsync(apiKey, baseUrl, parameters);
    }
}

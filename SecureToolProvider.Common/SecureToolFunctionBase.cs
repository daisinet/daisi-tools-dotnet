using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SecureToolProvider.Common.Models;

namespace SecureToolProvider.Common;

/// <summary>
/// Base class for secure tool Azure Functions. Provides standard implementations
/// of install/uninstall/configure/auth endpoints. Subclasses implement ExecuteToolAsync()
/// to provide the actual tool logic, and optionally override GetOAuthConfig() for OAuth tools.
/// </summary>
public abstract class SecureToolFunctionBase
{
    protected readonly ISetupStore SetupStore;
    protected readonly AuthValidator AuthValidator;
    protected readonly ILogger Logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    protected SecureToolFunctionBase(ISetupStore setupStore, AuthValidator authValidator, ILogger logger)
    {
        SetupStore = setupStore;
        AuthValidator = authValidator;
        Logger = logger;
    }

    /// <summary>
    /// Override to provide OAuth configuration for providers that use OAuth.
    /// Return null for API-key-only providers.
    /// </summary>
    protected virtual OAuthHelper? GetOAuthHelper() => null;

    /// <summary>
    /// Execute the tool with the given parameters and stored setup data.
    /// This is the main extension point for subclasses.
    /// </summary>
    protected abstract Task<ExecuteResponse> ExecuteToolAsync(
        string installId, string toolId, List<ParameterValue> parameters, Dictionary<string, string> setup);

    /// <summary>
    /// POST /api/install — Called by ORC when a user purchases the tool.
    /// </summary>
    protected async Task<HttpResponseData> HandleInstallAsync(HttpRequestData req)
    {
        if (!AuthValidator.VerifyDaisiAuth(req))
            return req.CreateResponse(HttpStatusCode.Unauthorized);

        var body = await DeserializeAsync<InstallRequest>(req);
        if (body is null || string.IsNullOrEmpty(body.InstallId))
            return await CreateJsonResponse(req, HttpStatusCode.BadRequest,
                new InstallResponse { Success = false, Error = "Invalid request body" });

        await SetupStore.RegisterInstallAsync(body.InstallId, body.ToolId);

        // Also register the BundleInstallId so OAuth can be shared across bundled tools
        if (!string.IsNullOrEmpty(body.BundleInstallId))
            await SetupStore.RegisterInstallAsync(body.BundleInstallId, $"bundle:{body.ToolId}");

        Logger.LogInformation("Installed tool {ToolId} with installId {InstallId}, bundleInstallId {BundleInstallId}",
            body.ToolId, body.InstallId, body.BundleInstallId ?? "(none)");

        return await CreateJsonResponse(req, HttpStatusCode.OK, new InstallResponse { Success = true });
    }

    /// <summary>
    /// POST /api/uninstall — Called by ORC when a purchase is deactivated.
    /// </summary>
    protected async Task<HttpResponseData> HandleUninstallAsync(HttpRequestData req)
    {
        if (!AuthValidator.VerifyDaisiAuth(req))
            return req.CreateResponse(HttpStatusCode.Unauthorized);

        var body = await DeserializeAsync<UninstallRequest>(req);
        if (body is null || string.IsNullOrEmpty(body.InstallId))
            return await CreateJsonResponse(req, HttpStatusCode.BadRequest,
                new UninstallResponse { Success = false });

        await SetupStore.RemoveInstallAsync(body.InstallId);
        Logger.LogInformation("Uninstalled installId {InstallId}", body.InstallId);

        return await CreateJsonResponse(req, HttpStatusCode.OK, new UninstallResponse { Success = true });
    }

    /// <summary>
    /// POST /api/configure — Receives user setup data and stores it.
    /// </summary>
    protected async Task<HttpResponseData> HandleConfigureAsync(HttpRequestData req)
    {
        var body = await DeserializeAsync<ConfigureRequest>(req);
        if (body is null || string.IsNullOrEmpty(body.InstallId))
            return await CreateJsonResponse(req, HttpStatusCode.BadRequest,
                new ConfigureResponse { Success = false, Error = "Invalid request body" });

        if (!await SetupStore.IsInstalledAsync(body.InstallId))
            return await CreateJsonResponse(req, HttpStatusCode.Forbidden,
                new ConfigureResponse { Success = false, Error = "Unknown installation. The tool may not be installed." });

        await SetupStore.SaveSetupAsync(body.InstallId, body.SetupValues);
        Logger.LogInformation("Configured tool {ToolId} for installId {InstallId}", body.ToolId, body.InstallId);

        return await CreateJsonResponse(req, HttpStatusCode.OK, new ConfigureResponse { Success = true });
    }

    /// <summary>
    /// POST /api/execute — Executes the tool with provided parameters.
    /// </summary>
    protected async Task<HttpResponseData> HandleExecuteAsync(HttpRequestData req)
    {
        var body = await DeserializeAsync<ExecuteRequest>(req);
        if (body is null || string.IsNullOrEmpty(body.InstallId))
            return await CreateJsonResponse(req, HttpStatusCode.BadRequest,
                new ExecuteResponse { Success = false, ErrorMessage = "Invalid request body" });

        if (!await SetupStore.IsInstalledAsync(body.InstallId))
            return await CreateJsonResponse(req, HttpStatusCode.Forbidden,
                new ExecuteResponse { Success = false, ErrorMessage = "Unknown installation. The tool may not be installed." });

        var setup = await SetupStore.GetSetupAsync(body.InstallId);
        if (setup is null)
            return await CreateJsonResponse(req, HttpStatusCode.OK,
                new ExecuteResponse { Success = false, ErrorMessage = "Tool has not been configured. Please configure it first." });

        // Check if OAuth tokens need refresh
        var oauthHelper = GetOAuthHelper();
        if (oauthHelper is not null)
        {
            setup = await RefreshTokensIfNeededAsync(body.InstallId, setup, oauthHelper);
        }

        try
        {
            var result = await ExecuteToolAsync(body.InstallId, body.ToolId, body.Parameters, setup);
            Logger.LogInformation("Executed tool {ToolId} for installId {InstallId}", body.ToolId, body.InstallId);
            return await CreateJsonResponse(req, HttpStatusCode.OK, result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing tool {ToolId} for installId {InstallId}", body.ToolId, body.InstallId);
            return await CreateJsonResponse(req, HttpStatusCode.OK,
                new ExecuteResponse { Success = false, ErrorMessage = $"Tool execution failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// GET or POST /api/auth/start — Initiates the OAuth flow.
    /// GET: reads installId/service from query params and redirects the browser to the OAuth provider.
    /// POST: reads from JSON body and returns the authorize URL as JSON.
    /// </summary>
    protected async Task<HttpResponseData> HandleAuthStartAsync(HttpRequestData req)
    {
        var oauthHelper = GetOAuthHelper();
        if (oauthHelper is null)
            return await CreateJsonResponse(req, HttpStatusCode.BadRequest,
                new AuthStartResponse { Success = false, Error = "This tool does not use OAuth." });

        string? installId;
        string? setupKey;

        if (req.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            installId = query["installId"];
            setupKey = query["service"] ?? string.Empty;
        }
        else
        {
            var body = await DeserializeAsync<AuthStartRequest>(req);
            installId = body?.InstallId;
            setupKey = body?.SetupKey ?? string.Empty;
        }

        if (string.IsNullOrEmpty(installId))
            return await CreateJsonResponse(req, HttpStatusCode.BadRequest,
                new AuthStartResponse { Success = false, Error = "Missing installId" });

        if (!await SetupStore.IsInstalledAsync(installId))
            return await CreateJsonResponse(req, HttpStatusCode.Forbidden,
                new AuthStartResponse { Success = false, Error = "Unknown installation." });

        var (authorizeUrl, _) = oauthHelper.BuildAuthorizeUrl(installId, setupKey);

        // GET requests redirect the browser directly to the OAuth provider
        if (req.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            var response = req.CreateResponse(HttpStatusCode.Redirect);
            response.Headers.Add("Location", authorizeUrl);
            return response;
        }

        return await CreateJsonResponse(req, HttpStatusCode.OK,
            new AuthStartResponse { Success = true, AuthorizeUrl = authorizeUrl });
    }

    /// <summary>
    /// GET /api/auth/callback — OAuth callback endpoint. Exchanges code for tokens and stores them.
    /// </summary>
    protected async Task<HttpResponseData> HandleAuthCallbackAsync(HttpRequestData req)
    {
        var oauthHelper = GetOAuthHelper();
        if (oauthHelper is null)
            return req.CreateResponse(HttpStatusCode.BadRequest);

        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var code = query["code"];
        var state = query["state"];
        var error = query["error"];

        if (!string.IsNullOrEmpty(error))
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.OK);
            errorResponse.Headers.Add("Content-Type", "text/html");
            await errorResponse.WriteStringAsync(BuildCallbackHtml(false, $"Authorization denied: {error}"));
            return errorResponse;
        }

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            var badReq = req.CreateResponse(HttpStatusCode.BadRequest);
            badReq.Headers.Add("Content-Type", "text/html");
            await badReq.WriteStringAsync(BuildCallbackHtml(false, "Missing code or state parameter."));
            return badReq;
        }

        var parsed = OAuthHelper.ParseState(state);
        if (parsed is null)
        {
            var badReq = req.CreateResponse(HttpStatusCode.BadRequest);
            badReq.Headers.Add("Content-Type", "text/html");
            await badReq.WriteStringAsync(BuildCallbackHtml(false, "Invalid state parameter."));
            return badReq;
        }

        var (installId, setupKey) = parsed.Value;

        var tokens = await oauthHelper.ExchangeCodeForTokensAsync(code, state);
        if (tokens is null)
        {
            var errorResp = req.CreateResponse(HttpStatusCode.OK);
            errorResp.Headers.Add("Content-Type", "text/html");
            await errorResp.WriteStringAsync(BuildCallbackHtml(false, "Failed to exchange authorization code for tokens."));
            return errorResp;
        }

        // Store tokens in setup data
        var setup = await SetupStore.GetSetupAsync(installId) ?? new Dictionary<string, string>();
        setup[$"{setupKey}_access_token"] = tokens.AccessToken;
        if (tokens.RefreshToken is not null)
            setup[$"{setupKey}_refresh_token"] = tokens.RefreshToken;
        setup[$"{setupKey}_expires_at"] = tokens.ExpiresAt.ToString("O");
        setup[$"{setupKey}_authenticated"] = "true";
        await SetupStore.SaveSetupAsync(installId, setup);

        Logger.LogInformation("OAuth completed for installId {InstallId}, setupKey {SetupKey}", installId, setupKey);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/html");
        await response.WriteStringAsync(BuildCallbackHtml(true, "Authorization successful! You can close this window."));
        return response;
    }

    /// <summary>
    /// POST /api/auth/status — Check if OAuth is complete for an installation.
    /// </summary>
    protected async Task<HttpResponseData> HandleAuthStatusAsync(HttpRequestData req)
    {
        var body = await DeserializeAsync<AuthStartRequest>(req);
        if (body is null || string.IsNullOrEmpty(body.InstallId))
            return await CreateJsonResponse(req, HttpStatusCode.BadRequest,
                new AuthStatusResponse { Success = false, Error = "Invalid request body" });

        var setup = await SetupStore.GetSetupAsync(body.InstallId);
        var isAuthenticated = setup is not null
            && setup.TryGetValue($"{body.SetupKey}_authenticated", out var auth)
            && auth == "true";

        return await CreateJsonResponse(req, HttpStatusCode.OK,
            new AuthStatusResponse { Success = true, IsAuthenticated = isAuthenticated });
    }

    /// <summary>
    /// Refresh OAuth tokens if they're expired.
    /// </summary>
    private async Task<Dictionary<string, string>> RefreshTokensIfNeededAsync(
        string installId, Dictionary<string, string> setup, OAuthHelper oauthHelper)
    {
        // Find all OAuth setup keys by looking for *_expires_at entries
        var oauthKeys = setup.Keys
            .Where(k => k.EndsWith("_expires_at"))
            .Select(k => k[..^"_expires_at".Length])
            .ToList();

        foreach (var key in oauthKeys)
        {
            if (!setup.TryGetValue($"{key}_expires_at", out var expiresAtStr))
                continue;

            if (!DateTimeOffset.TryParse(expiresAtStr, out var expiresAt))
                continue;

            // Check if expired (with 5-minute buffer)
            if (DateTimeOffset.UtcNow < expiresAt.AddMinutes(-5))
                continue;

            if (!setup.TryGetValue($"{key}_refresh_token", out var refreshToken))
                continue;

            Logger.LogInformation("Refreshing expired tokens for installId {InstallId}, key {Key}", installId, key);

            var newTokens = await oauthHelper.RefreshTokensAsync(refreshToken);
            if (newTokens is not null)
            {
                setup[$"{key}_access_token"] = newTokens.AccessToken;
                if (newTokens.RefreshToken is not null)
                    setup[$"{key}_refresh_token"] = newTokens.RefreshToken;
                setup[$"{key}_expires_at"] = newTokens.ExpiresAt.ToString("O");
                await SetupStore.SaveSetupAsync(installId, setup);
            }
        }

        return setup;
    }

    /// <summary>
    /// Helper to get the OAuth access token from setup data.
    /// </summary>
    protected static string? GetAccessToken(Dictionary<string, string> setup, string setupKey)
    {
        return setup.TryGetValue($"{setupKey}_access_token", out var token) ? token : null;
    }

    protected static async Task<T?> DeserializeAsync<T>(HttpRequestData req) where T : class
    {
        return await JsonSerializer.DeserializeAsync<T>(req.Body, JsonOptions);
    }

    protected static async Task<HttpResponseData> CreateJsonResponse<T>(
        HttpRequestData req, HttpStatusCode statusCode, T body)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(body);
        return response;
    }

    private static string BuildCallbackHtml(bool success, string message)
    {
        var color = success ? "#22c55e" : "#ef4444";
        var icon = success ? "&#10004;" : "&#10006;";
        return $$"""
            <!DOCTYPE html>
            <html>
            <head><title>Authorization</title></head>
            <body style="font-family:system-ui;display:flex;justify-content:center;align-items:center;height:100vh;margin:0;background:#f8fafc;">
                <div style="text-align:center;padding:2rem;">
                    <div style="font-size:3rem;color:{{color}};">{{icon}}</div>
                    <p style="font-size:1.2rem;color:#334155;">{{message}}</p>
                </div>
                <script>setTimeout(function(){ window.close(); }, 3000);</script>
            </body>
            </html>
            """;
    }
}

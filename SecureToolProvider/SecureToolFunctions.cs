using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

/// <summary>
/// Reference implementation of the Daisinet Secure Tool Provider API.
/// This example shows a simple "echo" tool that demonstrates the contract.
/// Replace the Execute logic with your actual tool implementation.
///
/// Endpoint authentication model:
/// - /install and /uninstall — ORC-originated, verified via X-Daisi-Auth header
/// - /configure and /execute — Consumer-originated (Manager UI / Host), verified via known InstallId
/// </summary>
public class SecureToolFunctions(ILogger<SecureToolFunctions> logger, SetupStore setupStore)
{
    private const string ExpectedAuthKey = "your-shared-secret-here"; // In production, use app settings

    /// <summary>
    /// POST /api/install
    /// Called by the ORC when a user purchases the tool. Registers the installation.
    /// Requires X-Daisi-Auth header.
    /// </summary>
    [Function("install")]
    public async Task<HttpResponseData> Install(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "install")] HttpRequestData req)
    {
        if (!VerifyAuth(req))
        {
            var unauthorized = req.CreateResponse(HttpStatusCode.Unauthorized);
            return unauthorized;
        }

        var body = await JsonSerializer.DeserializeAsync<InstallRequest>(req.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (body is null || string.IsNullOrEmpty(body.InstallId))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new InstallResponse { Success = false, Error = "Invalid request body" });
            return badRequest;
        }

        setupStore.RegisterInstall(body.InstallId, body.ToolId);

        logger.LogInformation("Installed tool {ToolId} with installId {InstallId}", body.ToolId, body.InstallId);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new InstallResponse { Success = true });
        return response;
    }

    /// <summary>
    /// POST /api/uninstall
    /// Called by the ORC when a purchase is deactivated. Cleans up stored data.
    /// Requires X-Daisi-Auth header.
    /// </summary>
    [Function("uninstall")]
    public async Task<HttpResponseData> Uninstall(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "uninstall")] HttpRequestData req)
    {
        if (!VerifyAuth(req))
        {
            var unauthorized = req.CreateResponse(HttpStatusCode.Unauthorized);
            return unauthorized;
        }

        var body = await JsonSerializer.DeserializeAsync<UninstallRequest>(req.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (body is null || string.IsNullOrEmpty(body.InstallId))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new UninstallResponse { Success = false });
            return badRequest;
        }

        setupStore.RemoveInstall(body.InstallId);

        logger.LogInformation("Uninstalled installId {InstallId}", body.InstallId);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new UninstallResponse { Success = true });
        return response;
    }

    /// <summary>
    /// POST /api/configure
    /// Receives user setup data (API keys, credentials) and stores them.
    /// Called directly by the Manager UI. Validated via known InstallId (no X-Daisi-Auth).
    /// </summary>
    [Function("configure")]
    public async Task<HttpResponseData> Configure(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "configure")] HttpRequestData req)
    {
        var body = await JsonSerializer.DeserializeAsync<ConfigureRequest>(req.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (body is null || string.IsNullOrEmpty(body.InstallId))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new ConfigureResponse { Success = false, Error = "Invalid request body" });
            return badRequest;
        }

        // Validate that this installId was registered via /install
        if (!setupStore.IsInstalled(body.InstallId))
        {
            var notFound = req.CreateResponse(HttpStatusCode.Forbidden);
            await notFound.WriteAsJsonAsync(new ConfigureResponse { Success = false, Error = "Unknown installation. The tool may not be installed." });
            return notFound;
        }

        // Store the setup values (in production, use secure storage like Azure Key Vault)
        setupStore.SaveSetup(body.InstallId, body.SetupValues);

        logger.LogInformation("Configured tool {ToolId} for installId {InstallId} with {Count} values",
            body.ToolId, body.InstallId, body.SetupValues.Count);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new ConfigureResponse { Success = true });
        return response;
    }

    /// <summary>
    /// POST /api/execute
    /// Executes the tool with the provided parameters.
    /// Uses the stored setup data for the requesting installation.
    /// Called directly by consumer hosts. Validated via known InstallId (no X-Daisi-Auth).
    /// </summary>
    [Function("execute")]
    public async Task<HttpResponseData> Execute(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "execute")] HttpRequestData req)
    {
        var body = await JsonSerializer.DeserializeAsync<ExecuteRequest>(req.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (body is null || string.IsNullOrEmpty(body.InstallId))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new ExecuteResponse { Success = false, ErrorMessage = "Invalid request body" });
            return badRequest;
        }

        // Validate that this installId was registered via /install
        if (!setupStore.IsInstalled(body.InstallId))
        {
            var notFound = req.CreateResponse(HttpStatusCode.Forbidden);
            await notFound.WriteAsJsonAsync(new ExecuteResponse { Success = false, ErrorMessage = "Unknown installation. The tool may not be installed." });
            return notFound;
        }

        // Retrieve stored setup for this installation
        var setup = setupStore.GetSetup(body.InstallId);
        if (setup is null)
        {
            var notConfigured = req.CreateResponse(HttpStatusCode.OK);
            await notConfigured.WriteAsJsonAsync(new ExecuteResponse
            {
                Success = false,
                ErrorMessage = "Tool has not been configured. Please configure it first."
            });
            return notConfigured;
        }

        // --- Your tool logic goes here ---
        // This example just echoes the parameters, setup keys, and OAuth status back.
        var paramSummary = string.Join(", ", body.Parameters.Select(p => $"{p.Name}={p.Value}"));
        var setupKeys = string.Join(", ", setup.Keys);

        // Check for OAuth connections
        var oauthInfo = new List<string>();
        foreach (var key in new[] { "office365", "google", "x" }) // Check common service names
        {
            if (setupStore.IsOAuthConnected(body.InstallId, key))
            {
                var tokens = setupStore.GetOAuthTokens(body.InstallId, key);
                oauthInfo.Add($"{key}: connected (expires {tokens?.ExpiresAt:u})");
            }
        }
        var oauthSummary = oauthInfo.Count > 0
            ? $"\nOAuth connections: {string.Join(", ", oauthInfo)}"
            : "\nNo OAuth connections";

        var output = $"Tool '{body.ToolId}' executed successfully.\n" +
                     $"Parameters: {paramSummary}\n" +
                     $"Setup keys available: {setupKeys}" +
                     oauthSummary;

        logger.LogInformation("Executed tool {ToolId} for installId {InstallId}", body.ToolId, body.InstallId);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new ExecuteResponse
        {
            Success = true,
            Output = output,
            OutputFormat = "plaintext",
            OutputMessage = $"Executed {body.ToolId}"
        });
        return response;
    }

    /// <summary>
    /// GET /api/auth/start
    /// OAuth initiation endpoint. The Manager UI opens this URL in a popup.
    /// In production, redirect to the external OAuth provider's consent screen (e.g. Microsoft, Google).
    /// In this reference implementation, it simulates by redirecting directly to the callback.
    /// </summary>
    [Function("auth-start")]
    public HttpResponseData AuthStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/start")] HttpRequestData req)
    {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var installId = query["installId"] ?? string.Empty;
        var returnUrl = query["returnUrl"] ?? string.Empty;
        var service = query["service"] ?? string.Empty;

        if (string.IsNullOrEmpty(installId) || string.IsNullOrEmpty(returnUrl))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            badRequest.WriteString("Missing installId or returnUrl query parameters.");
            return badRequest;
        }

        // Encode state for the callback
        var state = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(new { installId, returnUrl, service })));

        // In production: redirect to the external provider's OAuth consent URL, e.g.:
        //   https://login.microsoftonline.com/.../oauth2/v2.0/authorize?client_id=...&redirect_uri=...&state={state}
        // In this reference implementation, simulate by going directly to our own callback with a fake code.
        var callbackUrl = $"{req.Url.GetLeftPart(UriPartial.Authority)}/api/auth/callback?code=simulated-auth-code&state={Uri.EscapeDataString(state)}";

        logger.LogInformation("OAuth start for installId {InstallId}, service {Service} — redirecting to callback", installId, service);

        var response = req.CreateResponse(HttpStatusCode.Redirect);
        response.Headers.Add("Location", callbackUrl);
        return response;
    }

    /// <summary>
    /// GET /api/auth/callback
    /// OAuth callback endpoint. The external OAuth provider redirects here after user consent.
    /// Exchanges the authorization code for tokens and stores them.
    /// </summary>
    [Function("auth-callback")]
    public HttpResponseData AuthCallback(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/callback")] HttpRequestData req)
    {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var code = query["code"] ?? string.Empty;
        var stateEncoded = query["state"] ?? string.Empty;

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(stateEncoded))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            badRequest.WriteString("Missing code or state query parameters.");
            return badRequest;
        }

        // Decode the state to recover installId, returnUrl, service
        string installId, returnUrl, service;
        try
        {
            var stateJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(stateEncoded));
            var stateObj = JsonSerializer.Deserialize<JsonElement>(stateJson);
            installId = stateObj.GetProperty("installId").GetString() ?? string.Empty;
            returnUrl = stateObj.GetProperty("returnUrl").GetString() ?? string.Empty;
            service = stateObj.GetProperty("service").GetString() ?? string.Empty;
        }
        catch
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            badRequest.WriteString("Invalid state parameter.");
            return badRequest;
        }

        if (!setupStore.IsInstalled(installId))
        {
            var forbidden = req.CreateResponse(HttpStatusCode.Forbidden);
            forbidden.WriteString("Unknown installation.");
            return forbidden;
        }

        // In production: exchange the `code` for tokens using the provider's token endpoint
        // and your client_id + client_secret. For this reference impl, simulate tokens.
        var accessToken = $"simulated-access-token-{Guid.NewGuid():N}";
        var refreshToken = $"simulated-refresh-token-{Guid.NewGuid():N}";
        var expiresAt = DateTime.UtcNow.AddHours(1);

        setupStore.SaveOAuthTokens(installId, service, accessToken, refreshToken, expiresAt);

        logger.LogInformation("OAuth callback: stored tokens for installId {InstallId}, service {Service}", installId, service);

        // Redirect the popup back to the Manager's OAuthCallback page
        var response = req.CreateResponse(HttpStatusCode.Redirect);
        response.Headers.Add("Location", returnUrl);
        return response;
    }

    /// <summary>
    /// POST /api/auth/status
    /// Checks if an OAuth connection is active for a given installation and service.
    /// Called by the Manager UI to show connection status.
    /// </summary>
    [Function("auth-status")]
    public async Task<HttpResponseData> AuthStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/status")] HttpRequestData req)
    {
        var body = await JsonSerializer.DeserializeAsync<AuthStatusRequest>(req.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (body is null || string.IsNullOrEmpty(body.InstallId))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new AuthStatusResponse { Connected = false });
            return badRequest;
        }

        if (!setupStore.IsInstalled(body.InstallId))
        {
            var forbidden = req.CreateResponse(HttpStatusCode.Forbidden);
            await forbidden.WriteAsJsonAsync(new AuthStatusResponse { Connected = false });
            return forbidden;
        }

        var connected = setupStore.IsOAuthConnected(body.InstallId, body.Service);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new AuthStatusResponse
        {
            Connected = connected,
            ServiceName = body.Service,
            UserLabel = connected ? "Connected" : null
        });
        return response;
    }

    /// <summary>
    /// Verify the X-Daisi-Auth header matches the shared secret.
    /// Only used for ORC-originated calls (install/uninstall).
    /// </summary>
    private static bool VerifyAuth(HttpRequestData req)
    {
        if (req.Headers.TryGetValues("X-Daisi-Auth", out var values))
        {
            return values.Any(v => v == ExpectedAuthKey);
        }
        return false;
    }
}

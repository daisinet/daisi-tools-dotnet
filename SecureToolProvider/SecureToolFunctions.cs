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

        setupStore.RegisterInstall(body.InstallId, body.ToolId, body.BundleInstallId);

        logger.LogInformation("Installed tool {ToolId} with installId {InstallId}, bundleInstallId {BundleInstallId}",
            body.ToolId, body.InstallId, body.BundleInstallId ?? "(none)");

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
        // This example just echoes the parameters and setup keys back.
        var paramSummary = string.Join(", ", body.Parameters.Select(p => $"{p.Name}={p.Value}"));
        var setupKeys = string.Join(", ", setup.Keys);

        var output = $"Tool '{body.ToolId}' executed successfully.\n" +
                     $"Parameters: {paramSummary}\n" +
                     $"Setup keys available: {setupKeys}";

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

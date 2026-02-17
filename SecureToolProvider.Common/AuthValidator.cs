using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;

namespace SecureToolProvider.Common;

/// <summary>
/// Validates authentication for secure tool provider endpoints.
/// - ORC endpoints: validated via X-Daisi-Auth header (shared secret)
/// - Consumer endpoints: validated via InstallId (known installation)
/// </summary>
public class AuthValidator
{
    private readonly string _authKey;

    public AuthValidator(IConfiguration configuration)
    {
        _authKey = configuration["DaisiAuthKey"] ?? "your-shared-secret-here";
    }

    /// <summary>
    /// Verify the X-Daisi-Auth header matches the configured shared secret.
    /// Used for ORC-originated calls (install/uninstall).
    /// </summary>
    public bool VerifyDaisiAuth(HttpRequestData req)
    {
        if (req.Headers.TryGetValues("X-Daisi-Auth", out var values))
        {
            return values.Any(v => v == _authKey);
        }
        return false;
    }

    /// <summary>
    /// Verify that an installId is registered (known installation).
    /// Used for consumer-originated calls (configure/execute/auth).
    /// </summary>
    public async Task<bool> VerifyInstallIdAsync(string? installId, ISetupStore setupStore)
    {
        if (string.IsNullOrEmpty(installId))
            return false;

        return await setupStore.IsInstalledAsync(installId);
    }
}

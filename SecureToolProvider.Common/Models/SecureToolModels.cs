namespace SecureToolProvider.Common.Models;

/// <summary>
/// Request body for the /install endpoint (called by ORC on purchase).
/// </summary>
public class InstallRequest
{
    public string InstallId { get; set; } = string.Empty;
    public string ToolId { get; set; } = string.Empty;
    public string? BundleInstallId { get; set; }
}

/// <summary>
/// Response body for the /install endpoint.
/// </summary>
public class InstallResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Request body for the /uninstall endpoint (called by ORC on deactivation).
/// </summary>
public class UninstallRequest
{
    public string InstallId { get; set; } = string.Empty;
}

/// <summary>
/// Response body for the /uninstall endpoint.
/// </summary>
public class UninstallResponse
{
    public bool Success { get; set; }
}

/// <summary>
/// Request body for the /configure endpoint (called directly by Manager UI).
/// </summary>
public class ConfigureRequest
{
    public string InstallId { get; set; } = string.Empty;
    public string ToolId { get; set; } = string.Empty;
    public Dictionary<string, string> SetupValues { get; set; } = new();
}

/// <summary>
/// Response body for the /configure endpoint.
/// </summary>
public class ConfigureResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Request body for the /execute endpoint (called directly by consumer hosts).
/// </summary>
public class ExecuteRequest
{
    public string InstallId { get; set; } = string.Empty;
    public string ToolId { get; set; } = string.Empty;
    public List<ParameterValue> Parameters { get; set; } = [];
}

/// <summary>
/// A single name/value parameter.
/// </summary>
public class ParameterValue
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Response body for the /execute endpoint.
/// </summary>
public class ExecuteResponse
{
    public bool Success { get; set; }
    public string? Output { get; set; }
    public string OutputFormat { get; set; } = "plaintext";
    public string? OutputMessage { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Request body for the /auth/start endpoint (initiates OAuth flow).
/// </summary>
public class AuthStartRequest
{
    public string InstallId { get; set; } = string.Empty;
    public string SetupKey { get; set; } = string.Empty;
}

/// <summary>
/// Response body for the /auth/start endpoint.
/// </summary>
public class AuthStartResponse
{
    public bool Success { get; set; }
    public string? AuthorizeUrl { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Response body for the /auth/status endpoint.
/// </summary>
public class AuthStatusResponse
{
    public bool Success { get; set; }
    public bool IsAuthenticated { get; set; }
    public string? Error { get; set; }
}

namespace SecureToolProvider.Common;

/// <summary>
/// Interface for storing installation state and setup data.
/// </summary>
public interface ISetupStore
{
    /// <summary>
    /// Register an installation. Called when ORC notifies /install on purchase.
    /// </summary>
    Task RegisterInstallAsync(string installId, string toolId);

    /// <summary>
    /// Remove an installation and all associated data.
    /// </summary>
    Task<bool> RemoveInstallAsync(string installId);

    /// <summary>
    /// Check if an installId is registered.
    /// </summary>
    Task<bool> IsInstalledAsync(string installId);

    /// <summary>
    /// Store setup values for an installation.
    /// </summary>
    Task SaveSetupAsync(string installId, Dictionary<string, string> values);

    /// <summary>
    /// Retrieve stored setup values for an installation.
    /// </summary>
    Task<Dictionary<string, string>?> GetSetupAsync(string installId);

    /// <summary>
    /// Get the tool ID for an installation.
    /// </summary>
    Task<string?> GetToolIdAsync(string installId);
}

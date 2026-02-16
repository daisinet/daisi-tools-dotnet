using System.Collections.Concurrent;

/// <summary>
/// In-memory store for installation and setup data. In production, replace with Azure Key Vault,
/// encrypted database, or another secure storage mechanism.
/// </summary>
public class SetupStore
{
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _setupData = new();
    private readonly ConcurrentDictionary<string, string> _installations = new();

    /// <summary>
    /// Register an installation. Called when ORC notifies /install on purchase.
    /// </summary>
    public void RegisterInstall(string installId, string toolId)
    {
        _installations[installId] = toolId;
    }

    /// <summary>
    /// Remove an installation. Called when ORC notifies /uninstall on deactivation.
    /// </summary>
    public bool RemoveInstall(string installId)
    {
        _setupData.TryRemove(installId, out _);
        return _installations.TryRemove(installId, out _);
    }

    /// <summary>
    /// Check if an installId is registered (was installed via /install).
    /// </summary>
    public bool IsInstalled(string installId)
    {
        return _installations.ContainsKey(installId);
    }

    /// <summary>
    /// Store setup values for an installation.
    /// </summary>
    public void SaveSetup(string installId, Dictionary<string, string> values)
    {
        _setupData[installId] = values;
    }

    /// <summary>
    /// Retrieve stored setup values for an installation.
    /// </summary>
    public Dictionary<string, string>? GetSetup(string installId)
    {
        return _setupData.TryGetValue(installId, out var values) ? values : null;
    }
}

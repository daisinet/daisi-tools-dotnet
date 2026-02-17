using System.Collections.Concurrent;

/// <summary>
/// In-memory store for installation and setup data. In production, replace with Azure Key Vault,
/// encrypted database, or another secure storage mechanism.
/// </summary>
public class SetupStore
{
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _setupData = new();
    private readonly ConcurrentDictionary<string, string> _installations = new();
    private readonly ConcurrentDictionary<string, string> _bundleMap = new();
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _oauthTokens = new();

    /// <summary>
    /// Register an installation. Called when ORC notifies /install on purchase.
    /// Optionally associates the install with a shared bundle ID for OAuth.
    /// </summary>
    public void RegisterInstall(string installId, string toolId, string? bundleInstallId = null)
    {
        _installations[installId] = toolId;
        if (!string.IsNullOrEmpty(bundleInstallId))
            _bundleMap[installId] = bundleInstallId;
    }

    /// <summary>
    /// Remove an installation. Called when ORC notifies /uninstall on deactivation.
    /// </summary>
    public bool RemoveInstall(string installId)
    {
        _setupData.TryRemove(installId, out _);
        _bundleMap.TryRemove(installId, out _);
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
    /// Get the bundle install ID for an install, if one exists.
    /// </summary>
    public string? GetBundleInstallId(string installId)
    {
        return _bundleMap.TryGetValue(installId, out var bundleId) ? bundleId : null;
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

    /// <summary>
    /// Resolve the OAuth key for an install. If the install belongs to a bundle,
    /// uses the bundleInstallId so OAuth tokens are shared across all tools in the bundle.
    /// </summary>
    public string ResolveOAuthKey(string installId, string service)
    {
        var key = _bundleMap.TryGetValue(installId, out var bundleId) ? bundleId : installId;
        return $"{key}:{service}";
    }

    /// <summary>
    /// Store OAuth tokens keyed by the resolved OAuth key (bundle-aware).
    /// </summary>
    public void SaveOAuthTokens(string installId, string service, Dictionary<string, string> tokens)
    {
        var key = ResolveOAuthKey(installId, service);
        _oauthTokens[key] = tokens;
    }

    /// <summary>
    /// Retrieve OAuth tokens for an install. Resolves to bundle-level tokens if in a bundle.
    /// </summary>
    public Dictionary<string, string>? GetOAuthTokens(string installId, string service)
    {
        var key = ResolveOAuthKey(installId, service);
        return _oauthTokens.TryGetValue(key, out var tokens) ? tokens : null;
    }

    /// <summary>
    /// Check if OAuth tokens exist for an install (bundle-aware).
    /// </summary>
    public bool HasOAuthTokens(string installId, string service)
    {
        var key = ResolveOAuthKey(installId, service);
        return _oauthTokens.ContainsKey(key);
    }
}

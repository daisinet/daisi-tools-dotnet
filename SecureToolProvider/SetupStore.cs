using System.Collections.Concurrent;

/// <summary>
/// In-memory store for installation and setup data. In production, replace with Azure Key Vault,
/// encrypted database, or another secure storage mechanism.
/// </summary>
public class SetupStore
{
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _setupData = new();
    private readonly ConcurrentDictionary<string, string> _installations = new();
    private readonly ConcurrentDictionary<string, OAuthTokenData> _oauthTokens = new();

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

        // Remove all OAuth tokens for this installation
        foreach (var key in _oauthTokens.Keys)
        {
            if (key.StartsWith($"{installId}:"))
                _oauthTokens.TryRemove(key, out _);
        }

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

    /// <summary>
    /// Store OAuth tokens for a specific service connection.
    /// </summary>
    public void SaveOAuthTokens(string installId, string service, string accessToken, string refreshToken, DateTime expiresAt)
    {
        var key = $"{installId}:{service}";
        _oauthTokens[key] = new OAuthTokenData(accessToken, refreshToken, expiresAt);
    }

    /// <summary>
    /// Retrieve OAuth tokens for a specific service connection.
    /// </summary>
    public OAuthTokenData? GetOAuthTokens(string installId, string service)
    {
        var key = $"{installId}:{service}";
        return _oauthTokens.TryGetValue(key, out var data) ? data : null;
    }

    /// <summary>
    /// Check if an OAuth connection exists for a specific service.
    /// </summary>
    public bool IsOAuthConnected(string installId, string service)
    {
        var key = $"{installId}:{service}";
        return _oauthTokens.ContainsKey(key);
    }
}

/// <summary>
/// Stored OAuth token data for a service connection.
/// In production, encrypt these values at rest.
/// </summary>
public record OAuthTokenData(string AccessToken, string RefreshToken, DateTime ExpiresAt);

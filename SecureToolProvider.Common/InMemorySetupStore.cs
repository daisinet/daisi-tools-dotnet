using System.Collections.Concurrent;

namespace SecureToolProvider.Common;

/// <summary>
/// In-memory implementation of ISetupStore for local development and testing.
/// Data is lost on restart. Use PersistentSetupStore for production.
/// </summary>
public class InMemorySetupStore : ISetupStore
{
    private readonly ConcurrentDictionary<string, string> _installations = new();
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _setupData = new();

    public Task RegisterInstallAsync(string installId, string toolId)
    {
        _installations[installId] = toolId;
        return Task.CompletedTask;
    }

    public Task<bool> RemoveInstallAsync(string installId)
    {
        _setupData.TryRemove(installId, out _);
        var removed = _installations.TryRemove(installId, out _);
        return Task.FromResult(removed);
    }

    public Task<bool> IsInstalledAsync(string installId)
    {
        return Task.FromResult(_installations.ContainsKey(installId));
    }

    public Task SaveSetupAsync(string installId, Dictionary<string, string> values)
    {
        _setupData[installId] = values;
        return Task.CompletedTask;
    }

    public Task<Dictionary<string, string>?> GetSetupAsync(string installId)
    {
        var result = _setupData.TryGetValue(installId, out var values) ? values : null;
        return Task.FromResult(result);
    }

    public Task<string?> GetToolIdAsync(string installId)
    {
        var result = _installations.TryGetValue(installId, out var toolId) ? toolId : null;
        return Task.FromResult(result);
    }
}

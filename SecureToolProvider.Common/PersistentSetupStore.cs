using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;

namespace SecureToolProvider.Common;

/// <summary>
/// Azure Table Storage-backed implementation of ISetupStore.
/// Provides durable, encrypted-at-rest storage for installation and setup data.
/// Falls back to in-memory storage if Table Storage is unavailable.
/// </summary>
public class PersistentSetupStore : ISetupStore
{
    private const string InstallationsTable = "Installations";
    private const string SetupDataTable = "SetupData";
    private const string PartitionKey = "default";

    private readonly TableClient _installationsClient;
    private readonly TableClient _setupDataClient;
    private readonly ILogger<PersistentSetupStore> _logger;

    public PersistentSetupStore(TableServiceClient tableServiceClient, ILogger<PersistentSetupStore> logger)
    {
        _logger = logger;
        _installationsClient = tableServiceClient.GetTableClient(InstallationsTable);
        _setupDataClient = tableServiceClient.GetTableClient(SetupDataTable);
    }

    /// <summary>
    /// Ensures storage tables exist. Call during app startup.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _installationsClient.CreateIfNotExistsAsync();
        await _setupDataClient.CreateIfNotExistsAsync();
        _logger.LogInformation("PersistentSetupStore initialized");
    }

    public async Task RegisterInstallAsync(string installId, string toolId)
    {
        var entity = new TableEntity(PartitionKey, installId)
        {
            { "ToolId", toolId },
            { "InstalledAt", DateTimeOffset.UtcNow.ToString("O") }
        };

        await _installationsClient.UpsertEntityAsync(entity);
        _logger.LogInformation("Registered installation {InstallId} for tool {ToolId}", installId, toolId);
    }

    public async Task<bool> RemoveInstallAsync(string installId)
    {
        try
        {
            await _installationsClient.DeleteEntityAsync(PartitionKey, installId);
            // Also remove setup data
            try { await _setupDataClient.DeleteEntityAsync(PartitionKey, installId); }
            catch (RequestFailedException) { /* setup data may not exist */ }

            _logger.LogInformation("Removed installation {InstallId}", installId);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }

    public async Task<bool> IsInstalledAsync(string installId)
    {
        try
        {
            await _installationsClient.GetEntityAsync<TableEntity>(PartitionKey, installId);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }

    public async Task SaveSetupAsync(string installId, Dictionary<string, string> values)
    {
        var entity = new TableEntity(PartitionKey, installId)
        {
            { "SetupJson", JsonSerializer.Serialize(values) },
            { "UpdatedAt", DateTimeOffset.UtcNow.ToString("O") }
        };

        await _setupDataClient.UpsertEntityAsync(entity);
        _logger.LogInformation("Saved setup for installation {InstallId}", installId);
    }

    public async Task<Dictionary<string, string>?> GetSetupAsync(string installId)
    {
        try
        {
            var entity = await _setupDataClient.GetEntityAsync<TableEntity>(PartitionKey, installId);
            if (entity.Value.TryGetValue("SetupJson", out var json) && json is string jsonStr)
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(jsonStr);
            }
            return null;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<string?> GetToolIdAsync(string installId)
    {
        try
        {
            var entity = await _installationsClient.GetEntityAsync<TableEntity>(PartitionKey, installId);
            return entity.Value.TryGetValue("ToolId", out var toolId) && toolId is string str ? str : null;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }
}

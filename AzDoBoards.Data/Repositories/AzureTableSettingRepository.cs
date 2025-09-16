using Azure;
using Azure.Data.Tables;
using AzDoBoards.Data.Abstractions;
using AzDoBoards.Data.Entities;
using AzDoBoards.Utility;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace AzDoBoards.Data.Repositories;

/// <summary>
/// Azure Table Storage implementation of settings repository
/// </summary>
public class AzureTableSettingsRepository : ISettingsRepository
{
    private readonly TableClient _tableClient;
    private readonly ILogger<AzureTableSettingsRepository> _logger;

    public AzureTableSettingsRepository(TableServiceClient tableServiceClient, ILogger<AzureTableSettingsRepository> logger)
    {
        _tableClient = tableServiceClient.GetTableClient(Constants.Azure_StorageTableSetting);
        _logger = logger;
    }

    public async Task<string> GetOrCreateAsync(string key, string defaultValue, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(defaultValue);

        try
        {
            await _tableClient.CreateIfNotExistsAsync(cancellationToken);

            var response = await _tableClient.GetEntityIfExistsAsync<SettingTableEntity>("Settings", key, cancellationToken: cancellationToken);

            if (response.HasValue)
            {
                _logger.LogDebug("Retrieved setting {Key} with value length {ValueLength}", key, response.Value.Value.Length);
                return response.Value.Value;
            }

            // Create with default value
            var newEntity = new SettingTableEntity(key, defaultValue);
            await _tableClient.AddEntityAsync(newEntity, cancellationToken);

            _logger.LogInformation("Created new setting {Key} with default value", key);
            return defaultValue;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to get or create setting {Key}", key);
            throw;
        }
    }

    public async Task<Models.KeyValuePair> SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            await _tableClient.CreateIfNotExistsAsync(cancellationToken);

            var now = DateTimeOffset.UtcNow;
            var entity = new SettingTableEntity(key, value) { UpdatedAt = now };

            // Try to get existing entity to preserve CreatedAt
            var existingResponse = await _tableClient.GetEntityIfExistsAsync<SettingTableEntity>("Settings", key, cancellationToken: cancellationToken);
            if (existingResponse.HasValue)
            {
                entity.CreatedAt = existingResponse.Value.CreatedAt;
                entity.ETag = existingResponse.Value.ETag;
                await _tableClient.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace, cancellationToken);
                _logger.LogDebug("Updated existing setting {Key}", key);
            }
            else
            {
                entity.CreatedAt = now;
                await _tableClient.AddEntityAsync(entity, cancellationToken);
                _logger.LogDebug("Created new setting {Key}", key);
            }

            return entity.ToKeyValuePair();
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to set setting {Key}", key);
            throw;
        }
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            await _tableClient.CreateIfNotExistsAsync(cancellationToken);

            var response = await _tableClient.GetEntityIfExistsAsync<SettingTableEntity>("Settings", key, cancellationToken: cancellationToken);

            if (response.HasValue)
            {
                _logger.LogDebug("Retrieved setting {Key}", key);
                return response.Value.Value;
            }

            _logger.LogDebug("Setting {Key} not found", key);
            return null;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to get setting {Key}", key);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            await _tableClient.CreateIfNotExistsAsync(cancellationToken);

            var response = await _tableClient.GetEntityIfExistsAsync<SettingTableEntity>("Settings", key, cancellationToken: cancellationToken);
            return response.HasValue;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to check if setting {Key} exists", key);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            await _tableClient.CreateIfNotExistsAsync(cancellationToken);

            var response = await _tableClient.DeleteEntityAsync("Settings", key, cancellationToken: cancellationToken);

            _logger.LogInformation("Deleted setting {Key}", key);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogDebug("Setting {Key} not found for deletion", key);
            return false;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to delete setting {Key}", key);
            throw;
        }
    }

    public async Task<IEnumerable<Models.KeyValuePair>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _tableClient.CreateIfNotExistsAsync(cancellationToken);

            var entities = new List<Models.KeyValuePair>();

            await foreach (var entity in _tableClient.QueryAsync<SettingTableEntity>(
                filter: $"PartitionKey eq 'Settings'",
                cancellationToken: cancellationToken))
            {
                entities.Add(entity.ToKeyValuePair());
            }

            _logger.LogDebug("Retrieved {Count} settings", entities.Count);
            return entities;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to get all settings");
            throw;
        }
    }

    public async Task<T> GetSettingAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default)
    {
        var stringValue = await GetOrCreateAsync(key, ConvertToString(defaultValue), cancellationToken);
        return ConvertFromString<T>(stringValue);
    }

    public async Task SetSettingAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        await SetAsync(key, ConvertToString(value), cancellationToken);
    }

    private static string ConvertToString<T>(T value)
    {
        if (value == null) return string.Empty;

        var converter = TypeDescriptor.GetConverter(typeof(T));
        return converter.ConvertToInvariantString(value) ?? string.Empty;
    }

    private static T ConvertFromString<T>(string value)
    {
        if (string.IsNullOrEmpty(value) && typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
        {
            return default(T)!;
        }

        if (typeof(T) == typeof(string))
        {
            return (T)(object)value;
        }

        var converter = TypeDescriptor.GetConverter(typeof(T));
        return (T)converter.ConvertFromInvariantString(value)!;
    }
}
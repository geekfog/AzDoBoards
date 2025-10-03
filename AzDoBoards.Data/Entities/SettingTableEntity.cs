using Azure;
using Azure.Data.Tables;

namespace AzDoBoards.Data.Entities;

/// <summary>
/// Azure Table Storage entity for settings
/// </summary>
public class SettingTableEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "Settings";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    /// <summary>
    /// The setting value
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable notes about what this setting represents
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// When the setting was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the setting was last updated
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public SettingTableEntity() { }

    public SettingTableEntity(string key, string value, string notes = "")
    {
        RowKey = key;
        Value = value;
        Notes = notes;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Converts to domain model
    /// </summary>
    public Models.KeyValuePair ToKeyValuePair()
    {
        return new Models.KeyValuePair
        {
            Key = RowKey,
            Value = Value,
            Notes = Notes,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
    }

    /// <summary>
    /// Creates from domain model
    /// </summary>
    public static SettingTableEntity FromKeyValuePair(Models.KeyValuePair keyValuePair)
    {
        return new SettingTableEntity
        {
            RowKey = keyValuePair.Key,
            Value = keyValuePair.Value,
            Notes = keyValuePair.Notes,
            CreatedAt = keyValuePair.CreatedAt,
            UpdatedAt = keyValuePair.UpdatedAt
        };
    }
}
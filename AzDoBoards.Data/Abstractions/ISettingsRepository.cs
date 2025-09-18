namespace AzDoBoards.Data.Abstractions;

/// <summary>
/// Specialized repository for application settings
/// </summary>
public interface ISettingsRepository : IKeyValueRepository
{
    /// <summary>
    /// Gets a setting value with type conversion
    /// </summary>
    /// <typeparam name="T">The type to convert to</typeparam>
    /// <param name="key">Setting key</param>
    /// <param name="defaultValue">Default value if not found</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The converted value</returns>
    Task<T> GetSettingAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a setting value with type conversion
    /// </summary>
    /// <typeparam name="T">The type to convert from</typeparam>
    /// <param name="key">Setting key</param>
    /// <param name="value">Value to set</param>
    /// <param name="notes">Human-readable notes about what this setting represents</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetSettingAsync<T>(string key, T value, string? notes = null, CancellationToken cancellationToken = default);
}
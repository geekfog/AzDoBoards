using AzDoBoards.Data.Models;

namespace AzDoBoards.Data.Abstractions;

/// <summary>
/// Repository interface for key-value pair operations
/// </summary>
public interface IKeyValueRepository
{
    /// <summary>
    /// Gets a value by key. If not found, creates it with the default value and returns it.
    /// </summary>
    /// <param name="key">The key to search for</param>
    /// <param name="defaultValue">The default value to create if key doesn't exist</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The value for the key</returns>
    Task<string> GetOrCreateAsync(string key, string defaultValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value for a key. Creates the key if it doesn't exist, updates if it does.
    /// </summary>
    /// <param name="key">The key to set</param>
    /// <param name="value">The value to set</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated key-value pair</returns>
    Task<Models.KeyValuePair> SetAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value by key. Returns null if not found.
    /// </summary>
    /// <param name="key">The key to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The value for the key or null if not found</returns>
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a key exists
    /// </summary>
    /// <param name="key">The key to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if key exists, false otherwise</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a key-value pair
    /// </summary>
    /// <param name="key">The key to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all key-value pairs
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All key-value pairs</returns>
    Task<IEnumerable<Models.KeyValuePair>> GetAllAsync(CancellationToken cancellationToken = default);
}
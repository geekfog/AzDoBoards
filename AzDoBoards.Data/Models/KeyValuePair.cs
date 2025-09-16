namespace AzDoBoards.Data.Models;

/// <summary>
/// Represents a key-value pair entity for data storage
/// </summary>
public record KeyValuePair
{
    /// <summary>
    /// The unique key identifier
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// The value associated with the key
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// When the record was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the record was last updated
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
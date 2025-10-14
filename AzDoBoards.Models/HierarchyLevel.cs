namespace AzDoBoards.Models;

/// <summary>
/// Represents a hierarchy level with work item types and their target audiences
/// </summary>
public class HierarchyLevel
{
    /// <summary>
    /// Work item type names in this level
    /// </summary>
    public List<string> WorkItemTypes { get; set; } = [];

    /// <summary>
    /// Target audiences for this level (Roadmap, Planning, Building)
    /// </summary>
    public List<string> Audience { get; set; } = [];
}
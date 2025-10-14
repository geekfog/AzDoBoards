namespace AzDoBoards.Models.Roadmap;

/// <summary>
/// Types of dependencies between work items
/// </summary>
public enum DependencyType
{
    Predecessor, // Solid arrow line
    Related      // Dashed line
}

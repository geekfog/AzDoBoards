namespace AzDoBoards.Client.Models;

/// <summary>
/// Information about a work item type
/// </summary>
public class WorkItemTypeSummary
{
    public string ReferenceName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsDisabled { get; set; }
}

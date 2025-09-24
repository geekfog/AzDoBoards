namespace AzDoBoards.Client.Models;

/// <summary>
/// Information about a work item state
/// </summary>
public class WorkItemStateSummary
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsCompleted { get; set; }
    public List<string> WorkItemTypes { get; set; } = new();
}

/// <summary>
/// Grouped work item states by category
/// </summary>
public class WorkItemStateGroup
{
    public string Category { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<WorkItemStateSummary> States { get; set; } = new();
    public int Order { get; set; }
}
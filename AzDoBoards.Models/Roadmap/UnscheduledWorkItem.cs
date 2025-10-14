namespace AzDoBoards.Models.Roadmap;

/// <summary>
/// Represents an unscheduled work item in the roadmap
/// </summary>
public class UnscheduledWorkItem
{
    public int WorkItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string WorkItemType { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string StateCategory { get; set; } = string.Empty;
    public int ParentId { get; set; }
    public string ParentTitle { get; set; } = string.Empty;
    public string ParentType { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
}

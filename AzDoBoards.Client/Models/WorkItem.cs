namespace AzDoBoards.Client.Models;

/// <summary>
/// Represents a work item with all necessary properties for display
/// </summary>
public class WorkItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string WorkItemType { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string StateCategory { get; set; } = string.Empty;
    public string AssignedToDisplayName { get; set; } = string.Empty;
    public string AssignedToEmail { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime? TargetDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public string IterationPath { get; set; } = string.Empty;
    public string AreaPath { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string Color { get; set; } = string.Empty;
    
    // Additional properties for dynamic filtering
    public Dictionary<string, object> Fields { get; set; } = new();
}

/// <summary>
/// Filter parameters for work item queries
/// </summary>
public class WorkItemFilter
{
    public string ProjectId { get; set; } = string.Empty;
    public List<string> WorkItemTypes { get; set; } = new();
    public List<string> StateCategories { get; set; } = new();
    public List<string> States { get; set; } = new();
    public string AssignedToFilter { get; set; } = string.Empty;
    public string IterationFilter { get; set; } = string.Empty;
    public string AreaFilter { get; set; } = string.Empty;
    public DateTime? ModifiedSince { get; set; }
    public int Top { get; set; } = 200; // Default limit
}
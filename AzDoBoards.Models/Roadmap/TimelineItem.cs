namespace AzDoBoards.Models.Roadmap;

/// <summary>
/// Represents a roadmap timeline item positioned on the calendar
/// </summary>
public class TimelineItem
{
    public int WorkItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string WorkItemType { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public DateTime? TargetDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string State { get; set; } = string.Empty;
    public string StateCategory { get; set; } = string.Empty;
    public int ParentId { get; set; }
    public string ParentTitle { get; set; } = string.Empty;
    public string ParentType { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
    public List<int> Dependencies { get; set; } = new();
    public List<int> RelatedItems { get; set; } = new();
    public bool IsCollapsed { get; set; }

    // Position in timeline
    public double LeftPosition { get; set; }
    public double Width { get; set; }
    public int SwimLaneLevel { get; set; }
}

namespace AzDoBoards.Models.Roadmap;

/// <summary>
/// Represents a roadmap swimlane (hierarchical grouping)
/// </summary>
public class SwimLane
{
    public int WorkItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string WorkItemType { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool IsCollapsed { get; set; }
    public int Level { get; set; }
    public List<SwimLane> Children { get; set; } = new();
    public List<TimelineItem> TimelineItems { get; set; } = new();
}

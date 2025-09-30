namespace AzDoBoards.Utility.Models;

/// <summary>
/// Represents a roadmap timeline item positioned on the calendar
/// </summary>
public class RoadmapTimelineItem
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

/// <summary>
/// Represents a roadmap swimlane (hierarchical grouping)
/// </summary>
public class RoadmapSwimLane
{
    public int WorkItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string WorkItemType { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool IsCollapsed { get; set; }
    public int Level { get; set; }
    public List<RoadmapSwimLane> Children { get; set; } = new();
    public List<RoadmapTimelineItem> TimelineItems { get; set; } = new();
}

/// <summary>
/// Configuration for roadmap display
/// </summary>
public class RoadmapConfiguration
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public RoadmapTimeUnit TimeUnit { get; set; } = RoadmapTimeUnit.Week;
    public int ZoomLevel { get; set; } = 1;
    public bool ShowDependencies { get; set; } = true;
    public bool ShowRelated { get; set; } = true;
    public List<string> VisibleWorkItemTypes { get; set; } = new();
}

/// <summary>
/// Time units for roadmap timeline
/// </summary>
public enum RoadmapTimeUnit
{
    Day,
    Week,
    Month,
    Quarter
}

/// <summary>
/// Dependency relationship between work items
/// </summary>
public class WorkItemDependency
{
    public int PredecessorId { get; set; }
    public int SuccessorId { get; set; }
    public DependencyType Type { get; set; }
}

/// <summary>
/// Types of dependencies between work items
/// </summary>
public enum DependencyType
{
    Predecessor, // Solid arrow line
    Related      // Dashed line
}

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
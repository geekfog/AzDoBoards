namespace AzDoBoards.Models.Roadmap;

/// <summary>
/// Dependency relationship between work items
/// </summary>
public class WorkItemDependency
{
    public int PredecessorId { get; set; }
    public int SuccessorId { get; set; }
    public DependencyType Type { get; set; }
}

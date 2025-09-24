namespace AzDoBoards.Client.Models;

/// <summary>
/// Information about a process in Azure DevOps
/// </summary>
public class ProcessSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ReferenceName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsSystemProcess { get; set; }
    public int ProjectCount { get; set; }
    public Guid? ParentProcessId { get; set; }
    public string ParentProcessName { get; set; } = string.Empty;
}

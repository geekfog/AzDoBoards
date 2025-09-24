namespace AzDoBoards.Client.Models;

/// <summary>
/// Information about a project in Azure DevOps
/// </summary>
public class ProjectSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Visibility { get; set; } = string.Empty;
}

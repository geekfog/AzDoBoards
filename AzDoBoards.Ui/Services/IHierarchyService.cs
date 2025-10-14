using AzDoBoards.Client.Models;
using AzDoBoards.Models;

namespace AzDoBoards.Ui.Services;

/// <summary>
/// Client-side interface for hierarchy operations
/// </summary>
public interface IHierarchyService
{
    Task<string> GetCurrentProcessIdAsync();
    Task<string> GetCurrentProjectIdAsync(string processId);
    Task<List<WorkItemTypeSummary>?> LoadWorkItemTypesAsync(string processId);
    Task<HierarchyLevel[]?> LoadHierarchyLevelsAsync(string processId);
    Task<List<List<WorkItemTypeSummary>>> LoadHierarchyWithAudiencesAsync(string processId, List<WorkItemTypeSummary> availableWorkItemTypes);
}
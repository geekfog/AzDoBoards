using AzDoBoards.Client.Models;
using AzDoBoards.Models.Roadmap;
using AzDoBoards.Models;

namespace AzDoBoards.Ui.Services;

/// <summary>
/// Client-side interface for roadmap operations
/// </summary>
public interface IRoadmapService
{
    Task<(string ProcessId, string ProjectId)> GetConfigurationAsync();
    Task<List<HierarchyLevel>> GetRoadmapHierarchyLevelsAsync(string processId);
    Task<(List<string> TopLevel, List<string> ParentLevel, List<string> LowestLevel)> GetWorkItemTypesAsync(string processId);
    Task<List<WorkItem>> LoadRoadmapWorkItemsAsync(string projectId, string processId);
    Task<List<SwimLane>> BuildRoadmapSwimlanesAsync(List<WorkItem> workItems, string processId);
    Task<List<UnscheduledWorkItem>> GetUnscheduledWorkItemsAsync(List<WorkItem> workItems, string processId);
    Task<Config> GetDefaultConfigurationAsync();
    Task SaveConfigurationAsync(Config config);
    Task<bool> UpdateWorkItemTargetDateAsync(int workItemId, DateTime? targetDate);
    Task<bool> UpdateWorkItemStateAsync(int workItemId, string newState);
    Task<List<string>> GetAvailableStatesForWorkItemTypeAsync(string workItemType, string processId);
    List<TimelineItem> CalculateTimelinePositions(List<TimelineItem> timelineItems, Config config);
}
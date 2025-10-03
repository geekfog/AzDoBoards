using AzDoBoards.Client.Models;
using AzDoBoards.Utility.Models;

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
    Task<List<RoadmapSwimLane>> BuildRoadmapSwimlanesAsync(List<WorkItem> workItems, string processId);
    Task<List<UnscheduledWorkItem>> GetUnscheduledWorkItemsAsync(List<WorkItem> workItems, string processId);
    Task<RoadmapConfiguration> GetDefaultConfigurationAsync();
    Task SaveConfigurationAsync(RoadmapConfiguration config);
    Task<bool> UpdateWorkItemTargetDateAsync(int workItemId, DateTime? targetDate);
    List<RoadmapTimelineItem> CalculateTimelinePositions(List<RoadmapTimelineItem> timelineItems, RoadmapConfiguration config);
}
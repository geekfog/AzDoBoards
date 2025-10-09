using AzDoBoards.Client.Models;
using AzDoBoards.Client.Services;
using AzDoBoards.Data.Abstractions;
using AzDoBoards.Utility;
using AzDoBoards.Utility.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace AzDoBoards.Ui.Services;

/// <summary>
/// JavaScript-free implementation of roadmap service for Blazor Server
/// </summary>
public class JavaScriptFreeRoadmapService : IRoadmapService
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly JavaScriptFreeHierarchyService _hierarchyService;
    private readonly ILogger<JavaScriptFreeRoadmapService> _logger;

    public JavaScriptFreeRoadmapService(
        ISettingsRepository settingsRepository,
        IServiceProvider serviceProvider,
        IHierarchyService hierarchyService,
        ILogger<JavaScriptFreeRoadmapService> logger)
    {
        _settingsRepository = settingsRepository;
        _serviceProvider = serviceProvider;
        _hierarchyService = (JavaScriptFreeHierarchyService)hierarchyService;
        _logger = logger;
    }

    public async Task<(string ProcessId, string ProjectId)> GetConfigurationAsync()
    {
        var processId = await _hierarchyService.GetCurrentProcessIdAsync();
        var projectId = await _hierarchyService.GetCurrentProjectIdAsync(processId);
        return (processId, projectId);
    }

    public async Task<List<HierarchyLevel>> GetRoadmapHierarchyLevelsAsync(string processId)
    {
        try
        {
            var hierarchyLevels = await _hierarchyService.LoadHierarchyLevelsAsync(processId);
            if (hierarchyLevels == null)
                return new List<HierarchyLevel>();

            // Filter for roadmap audience only
            return hierarchyLevels
                .Where(level => level.Audience?.Contains("Roadmap", StringComparer.OrdinalIgnoreCase) == true)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roadmap hierarchy levels for process {ProcessId}", processId);
            return new List<HierarchyLevel>();
        }
    }

    public async Task<(List<string> TopLevel, List<string> ParentLevel, List<string> LowestLevel)> GetWorkItemTypesAsync(string processId)
    {
        var topLevel = await GetTopLevelWorkItemTypesAsync(processId);
        var parentLevel = await GetParentLevelWorkItemTypesAsync(processId);
        var lowestLevel = await GetLowestLevelWorkItemTypesAsync(processId);
        return (topLevel, parentLevel, lowestLevel);
    }

    public async Task<List<WorkItem>> LoadRoadmapWorkItemsAsync(string projectId, string processId)
    {
        try
        {
            var workItemService = _serviceProvider.GetRequiredService<WorkItemServices>();
            var hierarchyLevels = await GetRoadmapHierarchyLevelsAsync(processId);
            
            if (!hierarchyLevels.Any())
                return new List<WorkItem>();

            // Get all work item types for roadmap
            var allRoadmapWorkItemTypes = hierarchyLevels
                .SelectMany(level => level.WorkItemTypes)
                .Distinct()
                .ToList();

            var filter = new WorkItemFilter
            {
                ProjectId = projectId,
                WorkItemTypes = allRoadmapWorkItemTypes,
                StateCategories = new List<string> { "Proposed", "InProgress", "Completed" }, // Include all active states
                Top = 1000 // Increase limit for roadmap
            };

            return await workItemService.GetWorkItemsAsync(filter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading roadmap work items for project {ProjectId}", projectId);
            return new List<WorkItem>();
        }
    }

    public async Task<List<RoadmapSwimLane>> BuildRoadmapSwimlanesAsync(List<WorkItem> workItems, string processId)
    {
        var swimlanes = new List<RoadmapSwimLane>();
        
        try
        {
            var topLevelTypes = await GetTopLevelWorkItemTypesAsync(processId);
            var parentLevelTypes = await GetParentLevelWorkItemTypesAsync(processId);
            var lowestLevelTypes = await GetLowestLevelWorkItemTypesAsync(processId);

            _logger.LogInformation("Building swimlanes - Top: {TopTypes}, Parent: {ParentTypes}, Lowest: {LowestTypes}",
                string.Join(",", topLevelTypes), string.Join(",", parentLevelTypes), string.Join(",", lowestLevelTypes));

            // STEP 1: Create a dictionary of all work items for quick lookup
            var workItemLookup = workItems.ToDictionary(wi => wi.Id, wi => wi);

            // STEP 2: Get actual parent-child relationships from Azure DevOps
            var parentChildMap = await GetWorkItemRelationshipsAsync(workItems.Select(wi => wi.Id).ToList());

            // STEP 3: Group by top level work items (e.g., Initiatives)
            var topLevelItems = workItems
                .Where(wi => topLevelTypes.Contains(wi.WorkItemType, StringComparer.OrdinalIgnoreCase))
                .OrderBy(wi => wi.Title)
                .ToList();

            _logger.LogInformation("Found {TopLevelCount} top-level items", topLevelItems.Count);

            foreach (var topLevelItem in topLevelItems)
            {
                var swimlane = new RoadmapSwimLane
                {
                    WorkItemId = topLevelItem.Id,
                    Title = topLevelItem.Title,
                    WorkItemType = topLevelItem.WorkItemType,
                    Color = topLevelItem.Color,
                    Level = 0,
                    IsCollapsed = false,
                    Children = new List<RoadmapSwimLane>(),
                    TimelineItems = new List<RoadmapTimelineItem>()
                };

                // STEP 4: Find parent level items (e.g., Epics) that are children of this top level item
                var parentItemIds = parentChildMap.ContainsKey(topLevelItem.Id) ? parentChildMap[topLevelItem.Id] : new List<int>();
                var parentItems = parentItemIds
                    .Where(id => workItemLookup.ContainsKey(id))
                    .Select(id => workItemLookup[id])
                    .Where(wi => parentLevelTypes.Contains(wi.WorkItemType, StringComparer.OrdinalIgnoreCase))
                    .OrderBy(wi => wi.Title)
                    .ToList();

                _logger.LogInformation("Top-level item {TopItemId} '{TopItemTitle}' has {ParentCount} direct children: {ParentIds}",
                    topLevelItem.Id, topLevelItem.Title, parentItems.Count, string.Join(", ", parentItems.Select(p => p.Id)));

                foreach (var parentItem in parentItems)
                {
                    var childSwimlane = new RoadmapSwimLane
                    {
                        WorkItemId = parentItem.Id,
                        Title = parentItem.Title,
                        WorkItemType = parentItem.WorkItemType,
                        Color = parentItem.Color,
                        Level = 1,
                        IsCollapsed = false,
                        Children = new List<RoadmapSwimLane>(),
                        TimelineItems = new List<RoadmapTimelineItem>()
                    };

                    // STEP 5: Find timeline items (e.g., Features) that are children of this parent
                    var timelineItemIds = parentChildMap.ContainsKey(parentItem.Id) ? parentChildMap[parentItem.Id] : new List<int>();
                    var timelineItems = timelineItemIds
                        .Where(id => workItemLookup.ContainsKey(id))
                        .Select(id => workItemLookup[id])
                        .Where(wi => lowestLevelTypes.Contains(wi.WorkItemType, StringComparer.OrdinalIgnoreCase))
                        .Select(ConvertToTimelineItem)
                        .ToList();

                    _logger.LogInformation("Parent item {ParentItemId} '{ParentItemTitle}' has {TimelineItemCount} timeline items: {ItemIds}",
                        parentItem.Id, parentItem.Title, timelineItems.Count, 
                        string.Join(", ", timelineItems.Select(ti => $"{ti.WorkItemId}:{ti.Title}")));

                    childSwimlane.TimelineItems = timelineItems;
                    swimlane.Children.Add(childSwimlane);
                }

                _logger.LogInformation("Adding swimlane {SwimlaneName} with {ChildCount} children",
                    swimlane.Title, swimlane.Children.Count);

                swimlanes.Add(swimlane);
            }

            _logger.LogInformation("Built {TotalSwimlanes} total swimlanes", swimlanes.Count);
            return swimlanes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building roadmap swimlanes");
            return new List<RoadmapSwimLane>();
        }
    }

    public async Task<List<UnscheduledWorkItem>> GetUnscheduledWorkItemsAsync(List<WorkItem> workItems, string processId)
    {
        try
        {
            var lowestLevelTypes = await GetLowestLevelWorkItemTypesAsync(processId);
            var parentLevelTypes = await GetParentLevelWorkItemTypesAsync(processId);

            var unscheduledItems = workItems
                .Where(wi => lowestLevelTypes.Contains(wi.WorkItemType, StringComparer.OrdinalIgnoreCase) && 
                            !wi.TargetDate.HasValue &&
                            wi.StateCategory != "Completed")
                .Select(wi => new UnscheduledWorkItem
                {
                    WorkItemId = wi.Id,
                    Title = wi.Title,
                    WorkItemType = wi.WorkItemType,
                    Color = wi.Color,
                    State = wi.State,
                    StateCategory = wi.StateCategory,
                    AssignedTo = wi.AssignedToDisplayName,
                    ParentId = GetParentWorkItemId(workItems, wi.Id, parentLevelTypes),
                    ParentTitle = GetParentWorkItemTitle(workItems, wi.Id, parentLevelTypes),
                    ParentType = GetParentWorkItemType(workItems, wi.Id, parentLevelTypes)
                })
                .OrderBy(ui => ui.ParentTitle)
                .ThenBy(ui => ui.Title)
                .ToList();

            return unscheduledItems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unscheduled work items");
            return new List<UnscheduledWorkItem>();
        }
    }

    public async Task<RoadmapConfiguration> GetDefaultConfigurationAsync()
    {
        try
        {
            var startDateStr = await _settingsRepository.GetOrCreateAsync("roadmap-start-date", 
                DateTime.Today.AddMonths(-1).ToString("yyyy-MM-dd"));
            var endDateStr = await _settingsRepository.GetOrCreateAsync("roadmap-end-date", 
                DateTime.Today.AddMonths(11).ToString("yyyy-MM-dd"));
            
            var timeUnitStr = await _settingsRepository.GetOrCreateAsync("roadmap-time-unit", "Week");
            
            DateTime.TryParse(startDateStr, out var startDate);
            DateTime.TryParse(endDateStr, out var endDate);
            Enum.TryParse<RoadmapTimeUnit>(timeUnitStr, out var timeUnit);

            return new RoadmapConfiguration
            {
                StartDate = startDate == default ? DateTime.Today.AddMonths(-1) : startDate,
                EndDate = endDate == default ? DateTime.Today.AddMonths(11) : endDate,
                TimeUnit = timeUnit,
                ZoomLevel = 1,
                ShowDependencies = true,
                ShowRelated = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default roadmap configuration");
            return new RoadmapConfiguration
            {
                StartDate = DateTime.Today.AddMonths(-1),
                EndDate = DateTime.Today.AddMonths(11),
                TimeUnit = RoadmapTimeUnit.Week
            };
        }
    }

    public async Task SaveConfigurationAsync(RoadmapConfiguration config)
    {
        try
        {
            await _settingsRepository.SetAsync("roadmap-start-date", 
                config.StartDate.ToString("yyyy-MM-dd"), "Roadmap Configuration");
            await _settingsRepository.SetAsync("roadmap-end-date", 
                config.EndDate.ToString("yyyy-MM-dd"), "Roadmap Configuration");
            await _settingsRepository.SetAsync("roadmap-time-unit", 
                config.TimeUnit.ToString(), "Roadmap Configuration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving roadmap configuration");
        }
    }

    public async Task<bool> UpdateWorkItemTargetDateAsync(int workItemId, DateTime? targetDate)
    {
        try
        {
            var workItemService = _serviceProvider.GetRequiredService<WorkItemServices>();
            var success = await workItemService.UpdateWorkItemDatesAsync(workItemId, null, targetDate);
            
            if (success)
            {
                _logger.LogInformation("Successfully updated work item {WorkItemId} target date to {TargetDate}", 
                    workItemId, targetDate?.ToString("yyyy-MM-dd") ?? "null");
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to update work item {WorkItemId} target date", workItemId);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating work item {WorkItemId} target date", workItemId);
            return false;
        }
    }

    public List<RoadmapTimelineItem> CalculateTimelinePositions(
        List<RoadmapTimelineItem> timelineItems, 
        RoadmapConfiguration config)
    {
        try
        {
            var totalDays = (config.EndDate - config.StartDate).TotalDays;
            var timelineWidth = 100.0; // Percentage based

            foreach (var item in timelineItems.Where(ti => ti.TargetDate.HasValue))
            {
                var daysFromStart = (item.TargetDate!.Value - config.StartDate).TotalDays;
                item.LeftPosition = (daysFromStart / totalDays) * timelineWidth;
                
                // Set default width based on time unit
                item.Width = config.TimeUnit switch
                {
                    RoadmapTimeUnit.Day => 1.0,
                    RoadmapTimeUnit.Week => 7.0 / totalDays * timelineWidth,
                    RoadmapTimeUnit.Month => 30.0 / totalDays * timelineWidth,
                    RoadmapTimeUnit.Quarter => 90.0 / totalDays * timelineWidth,
                    _ => 7.0 / totalDays * timelineWidth
                };

                // Ensure minimum width for visibility
                item.Width = Math.Max(item.Width, 0.5);
            }

            return timelineItems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating timeline positions");
            return timelineItems;
        }
    }

    #region Private Helper Methods

    private async Task<List<string>> GetTopLevelWorkItemTypesAsync(string processId)
    {
        var hierarchyLevels = await GetRoadmapHierarchyLevelsAsync(processId);
        if (!hierarchyLevels.Any())
            return new List<string>();

        // The top level is the first level in the hierarchy
        var topLevel = hierarchyLevels.FirstOrDefault();
        return topLevel?.WorkItemTypes ?? new List<string>();
    }

    private async Task<List<string>> GetParentLevelWorkItemTypesAsync(string processId)
    {
        var hierarchyLevels = await GetRoadmapHierarchyLevelsAsync(processId);
        if (hierarchyLevels.Count < 2)
            return new List<string>();

        // The parent level is the second-to-last level in the hierarchy
        var parentLevel = hierarchyLevels[^2]; // C# 8.0 index from end syntax
        return parentLevel?.WorkItemTypes ?? new List<string>();
    }

    private async Task<List<string>> GetLowestLevelWorkItemTypesAsync(string processId)
    {
        var hierarchyLevels = await GetRoadmapHierarchyLevelsAsync(processId);
        if (!hierarchyLevels.Any())
            return new List<string>();

        // The lowest level is the last level in the hierarchy
        var lowestLevel = hierarchyLevels.LastOrDefault();
        return lowestLevel?.WorkItemTypes ?? new List<string>();
    }

    /// <summary>
    /// Gets actual work item relationships from Azure DevOps API
    /// </summary>
    /// <param name="workItemIds">List of work item IDs to get relationships for</param>
    /// <returns>Dictionary mapping parent work item ID to list of child work item IDs</returns>
    private async Task<Dictionary<int, List<int>>> GetWorkItemRelationshipsAsync(List<int> workItemIds)
    {
        var relationships = new Dictionary<int, List<int>>();
        
        try
        {
            var workItemService = _serviceProvider.GetRequiredService<WorkItemServices>();
            var parentChildMap = await workItemService.GetWorkItemRelationshipsAsync(workItemIds);
            
            _logger.LogInformation("Retrieved {RelationshipCount} parent-child relationships from Azure DevOps API", 
                parentChildMap.Sum(kvp => kvp.Value.Count));
                
            return parentChildMap;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting work item relationships from Azure DevOps API, falling back to simple heuristics");
            
            // Fallback to simple heuristics if the API call fails
            return GetWorkItemRelationshipsFallback(workItemIds);
        }
    }

    /// <summary>
    /// Simple fallback for work item relationships when API isn't available
    /// </summary>
    private Dictionary<int, List<int>> GetWorkItemRelationshipsFallback(List<int> workItemIds)
    {
        var relationships = new Dictionary<int, List<int>>();
        
        // This is a temporary implementation - in a real scenario you'd want better logic here
        // For now, just return empty relationships to avoid the previous bugs
        _logger.LogWarning("Using empty fallback for work item relationships");
        
        return relationships;
    }

    private int GetParentWorkItemId(List<WorkItem> workItems, int workItemId, List<string> parentTypes)
    {
        var workItem = workItems.FirstOrDefault(wi => wi.Id == workItemId);
        if (workItem == null) return 0;

        // First try to use the actual System.Parent field
        if (workItem.Fields.TryGetValue("System.Parent", out var parentField))
        {
            if (parentField is int parentFieldInt)
            {
                return parentFieldInt;
            }
            else if (parentField is string parentFieldStr && int.TryParse(parentFieldStr, out var parsedParentId))
            {
                return parsedParentId;
            }
        }

        // Fallback to heuristic based on area path and work item type
        return workItems
            .Where(wi => parentTypes.Contains(wi.WorkItemType, StringComparer.OrdinalIgnoreCase) &&
                        wi.AreaPath == workItem.AreaPath &&
                        wi.IterationPath == workItem.IterationPath)
            .FirstOrDefault()?.Id ?? 0;
    }

    private string GetParentWorkItemTitle(List<WorkItem> workItems, int workItemId, List<string> parentTypes)
    {
        var parentId = GetParentWorkItemId(workItems, workItemId, parentTypes);
        return workItems.FirstOrDefault(wi => wi.Id == parentId)?.Title ?? string.Empty;
    }

    private string GetParentWorkItemType(List<WorkItem> workItems, int workItemId, List<string> parentTypes)
    {
        var parentId = GetParentWorkItemId(workItems, workItemId, parentTypes);
        return workItems.FirstOrDefault(wi => wi.Id == parentId)?.WorkItemType ?? string.Empty;
    }

    private RoadmapTimelineItem ConvertToTimelineItem(WorkItem workItem)
    {
        return new RoadmapTimelineItem
        {
            WorkItemId = workItem.Id,
            Title = workItem.Title,
            WorkItemType = workItem.WorkItemType,
            Color = workItem.Color,
            TargetDate = workItem.TargetDate,
            State = workItem.State,
            StateCategory = workItem.StateCategory,
            AssignedTo = workItem.AssignedToDisplayName
        };
    }

    #endregion
}
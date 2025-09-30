using AzDoBoards.Client.Models;
using AzDoBoards.Client.Services;
using AzDoBoards.Data.Abstractions;
using AzDoBoards.Utility;
using AzDoBoards.Utility.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace AzDoBoards.Ui.Services;

/// <summary>
/// Service for managing work item hierarchy data
/// </summary>
public class HierarchyService
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HierarchyService> _logger;

    public HierarchyService(
        ISettingsRepository settingsRepository,
        IServiceProvider serviceProvider,
        ILogger<HierarchyService> logger)
    {
        _settingsRepository = settingsRepository;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Loads the current process ID from settings
    /// </summary>
    public async Task<string> GetCurrentProcessIdAsync()
    {
        return await _settingsRepository.GetOrCreateAsync("process", string.Empty);
    }

    /// <summary>
    /// Loads the current project ID for a given process
    /// </summary>
    public async Task<string> GetCurrentProjectIdAsync(string processId)
    {
        if (string.IsNullOrEmpty(processId))
            return string.Empty;

        var projectKey = $"project-{processId}";
        return await _settingsRepository.GetOrCreateAsync(projectKey, string.Empty);
    }

    /// <summary>
    /// Loads work item types for the current process
    /// </summary>
    public async Task<List<WorkItemTypeSummary>?> LoadWorkItemTypesAsync(string processId)
    {
        if (string.IsNullOrEmpty(processId) || !Guid.TryParse(processId, out var processGuid))
            return null;

        try
        {
            var authProvider = _serviceProvider.GetRequiredService<AuthenticationStateProvider>();
            var authState = await authProvider.GetAuthenticationStateAsync();

            if (!authState.User.Identity?.IsAuthenticated ?? false)
                return null;

            var processClient = _serviceProvider.GetRequiredService<ProcessServices>();
            return await processClient.GetWorkItemTypesForProcessAsync(processGuid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading work item types for process {ProcessId}", processId);
            return null;
        }
    }

    /// <summary>
    /// Loads the work item hierarchy for the current process (legacy format)
    /// </summary>
    public async Task<List<List<WorkItemTypeSummary>>> LoadHierarchyAsync(string processId, List<WorkItemTypeSummary> availableWorkItemTypes)
    {
        var hierarchy = new List<List<WorkItemTypeSummary>>();

        try
        {
            var hierarchyKey = $"work-item-hierarchy-{processId}";
            var hierarchyJson = await _settingsRepository.GetOrCreateAsync(hierarchyKey, "[]");

            var hierarchyData = WorkItemHelper.ParseHierarchyJson(hierarchyJson);
            if (hierarchyData != null && availableWorkItemTypes != null)
            {
                foreach (var level in hierarchyData)
                {
                    var levelItems = new List<WorkItemTypeSummary>();
                    foreach (var workItemName in level)
                    {
                        var workItem = availableWorkItemTypes.FirstOrDefault(w => w.Name == workItemName);
                        if (workItem != null)
                        {
                            levelItems.Add(workItem);
                        }
                    }
                    if (levelItems.Any())
                    {
                        hierarchy.Add(levelItems);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading work item hierarchy for process {ProcessId}", processId);
        }

        return hierarchy;
    }

    /// <summary>
    /// Loads the work item hierarchy levels with audience information
    /// </summary>
    public async Task<HierarchyLevel[]?> LoadHierarchyLevelsAsync(string processId)
    {
        try
        {
            var hierarchyKey = $"work-item-hierarchy-{processId}";
            var hierarchyJson = await _settingsRepository.GetOrCreateAsync(hierarchyKey, "[]");

            return WorkItemHelper.ParseHierarchyLevelsJson(hierarchyJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading work item hierarchy levels for process {ProcessId}", processId);
            return null;
        }
    }

    /// <summary>
    /// Loads the complete hierarchy with audience information and converts to legacy format
    /// </summary>
    public async Task<List<List<WorkItemTypeSummary>>> LoadHierarchyWithAudiencesAsync(string processId, List<WorkItemTypeSummary> availableWorkItemTypes)
    {
        try
        {
            // Load hierarchy levels with audiences
            var hierarchyLevels = await LoadHierarchyLevelsAsync(processId);
            if (hierarchyLevels == null || !availableWorkItemTypes.Any())
                return new List<List<WorkItemTypeSummary>>();

            // Convert to legacy format for backward compatibility
            return ConvertToLegacyHierarchy(hierarchyLevels, availableWorkItemTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading hierarchy with audiences for process {ProcessId}", processId);
            return new List<List<WorkItemTypeSummary>>();
        }
    }

    /// <summary>
    /// Gets hierarchy levels filtered by specific audiences
    /// </summary>
    public async Task<List<List<WorkItemTypeSummary>>> GetHierarchyByAudienceAsync(
        string processId, 
        List<WorkItemTypeSummary> availableWorkItemTypes, 
        params string[] targetAudiences)
    {
        try
        {
            var hierarchyLevels = await LoadHierarchyLevelsAsync(processId);
            if (hierarchyLevels == null || !availableWorkItemTypes.Any())
                return new List<List<WorkItemTypeSummary>>();

            var filteredHierarchy = new List<List<WorkItemTypeSummary>>();

            foreach (var level in hierarchyLevels)
            {
                // Check if this level has any of the target audiences
                if (level.Audience?.Any(a => targetAudiences.Contains(a)) == true)
                {
                    var levelItems = new List<WorkItemTypeSummary>();
                    foreach (var workItemName in level.WorkItemTypes)
                    {
                        var workItem = availableWorkItemTypes.FirstOrDefault(w => w.Name == workItemName);
                        if (workItem != null)
                        {
                            levelItems.Add(workItem);
                        }
                    }
                    if (levelItems.Any())
                    {
                        filteredHierarchy.Add(levelItems);
                    }
                }
            }

            return filteredHierarchy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading hierarchy by audience for process {ProcessId}", processId);
            return new List<List<WorkItemTypeSummary>>();
        }
    }

    /// <summary>
    /// Converts hierarchy levels back to the legacy format for compatibility
    /// </summary>
    public List<List<WorkItemTypeSummary>> ConvertToLegacyHierarchy(
        HierarchyLevel[] hierarchyLevels, 
        List<WorkItemTypeSummary> availableWorkItemTypes)
    {
        var hierarchy = new List<List<WorkItemTypeSummary>>();

        foreach (var level in hierarchyLevels)
        {
            var levelItems = new List<WorkItemTypeSummary>();
            foreach (var workItemName in level.WorkItemTypes)
            {
                var workItem = availableWorkItemTypes.FirstOrDefault(w => w.Name == workItemName);
                if (workItem != null)
                {
                    levelItems.Add(workItem);
                }
            }
            if (levelItems.Any())
            {
                hierarchy.Add(levelItems);
            }
        }

        return hierarchy;
    }
}
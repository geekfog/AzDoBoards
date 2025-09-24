using AzDoBoards.Client.Models;
using AzDoBoards.Client.Services;
using AzDoBoards.Data.Abstractions;
using AzDoBoards.Utility;
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
    /// Loads the work item hierarchy for the current process
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
}
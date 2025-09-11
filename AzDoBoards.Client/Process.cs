using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi.Models;

namespace AzDoBoards.Client;

public class Process(ConnectionFactory connectionFactory) : Base(connectionFactory)
{
    private Dictionary<Guid, ProcessInfo>? _processCache;
    private Dictionary<Guid, int>? _processProjectCountCache;
    private Dictionary<Guid, string>? _processToSampleProjectCache;

    /// <summary>
    /// Gets all processes in the Azure DevOps organization using the Process API (cached)
    /// </summary>
    /// <returns>List of processes with their details</returns>
    public async Task<List<ProcessInfo>> GetProcessesAsync()
    {
        if (_processCache != null)
            return [.. _processCache.Values];

        await LoadProcessDataAsync();
        return [.. _processCache!.Values];
    }

    /// <summary>
    /// Gets the count of team projects using each process (cached)
    /// </summary>
    /// <returns>Dictionary with process ID as key and project count as value</returns>
    public async Task<Dictionary<Guid, int>> GetProjectCountByProcessAsync()
    {
        if (_processProjectCountCache != null)
            return _processProjectCountCache;

        await LoadProcessDataAsync();
        return _processProjectCountCache!;
    }

    /// <summary>
    /// Gets unique work item types for a specific process (highly optimized)
    /// </summary>
    /// <param name="processId">The ID of the process</param>
    /// <returns>List of unique work item types for the specified process</returns>
    public async Task<List<WorkItemTypeInfo>> GetWorkItemTypesForProcessAsync(Guid processId)
    {
        var connection = await _connectionFactory.GetConnectionAsync();

        try
        {
            // Try to get work item types directly from the process (for inherited/custom processes)
            var processClient = connection.GetClient<WorkItemTrackingProcessHttpClient>();
            var workItemTypes = await processClient.GetProcessWorkItemTypesAsync(processId);

            return [.. workItemTypes.Select(wit => new WorkItemTypeInfo
            {
                ReferenceName = wit.ReferenceName,
                Name = wit.Name,
                Description = wit.Description,
                Color = wit.Color,
                Icon = wit.Icon ?? string.Empty,
                IsDisabled = wit.IsDisabled
            })];
        }
        catch
        {
            // Fallback: If direct process API fails (for system processes), use a project that uses this process
            await LoadProcessDataAsync();

            if (!_processToSampleProjectCache!.TryGetValue(processId, out var sampleProjectName))
            {
                return []; // No projects found for this process
            }

            var workItemClient = connection.GetClient<WorkItemTrackingHttpClient>();
            var projectWorkItemTypes = await workItemClient.GetWorkItemTypesAsync(sampleProjectName);

            return [.. projectWorkItemTypes.Select(wit => new WorkItemTypeInfo
            {
                ReferenceName = wit.ReferenceName,
                Name = wit.Name,
                Description = wit.Description,
                Color = wit.Color,
                Icon = wit.Icon?.Id ?? string.Empty,
                IsDisabled = wit.IsDisabled
            })];
        }
    }

    /// <summary>
    /// Gets processes with their associated project counts (cached)
    /// </summary>
    /// <returns>List of processes with project count information</returns>
    public async Task<List<ProcessWithProjectCount>> GetProcessesWithProjectCountsAsync()
    {
        var processes = await GetProcessesAsync();
        var projectCounts = await GetProjectCountByProcessAsync();

        return [.. processes.Select(p => new ProcessWithProjectCount
        {
            Process = p,
            ProjectCount = projectCounts.GetValueOrDefault(p.Id, 0)
        })];
    }

    /// <summary>
    /// Loads and caches all process data in a single pass for efficiency
    /// </summary>
    private async Task LoadProcessDataAsync()
    {
        if (_processCache != null) return; // Already loaded

        var connection = await _connectionFactory.GetConnectionAsync();

        // Get all processes using the Process API
        var processClient = connection.GetClient<WorkItemTrackingProcessHttpClient>();
        var allProcesses = await processClient.GetListOfProcessesAsync();

        var processes = new Dictionary<Guid, ProcessInfo>();
        foreach (var process in allProcesses)
        {
            var processInfo = new ProcessInfo
            {
                Id = process.TypeId,
                Name = process.Name,
                Description = process.Description,
                ReferenceName = process.ReferenceName,
                IsDefault = process.IsDefault,
                IsEnabled = process.IsEnabled,
                IsSystemProcess = process.ParentProcessTypeId == null // System processes don't have a parent
            };
            processes[process.TypeId] = processInfo;
        }

        // Get project counts and sample projects
        var projectClient = connection.GetClient<ProjectHttpClient>();
        var projects = await projectClient.GetProjects();

        var processProjectCounts = new Dictionary<Guid, int>();
        var processToSampleProject = new Dictionary<Guid, string>();

        foreach (var project in projects)
        {
            var projectDetail = await projectClient.GetProject(project.Id.ToString(), includeCapabilities: true);

            if (projectDetail.Capabilities?.TryGetValue("processTemplate", out var processTemplate) == true &&
                processTemplate.TryGetValue("templateTypeId", out var templateTypeId) &&
                Guid.TryParse(templateTypeId, out var processId))
            {
                // Update project count
                processProjectCounts[processId] = processProjectCounts.GetValueOrDefault(processId, 0) + 1;

                // Store first project name as sample (for getting work item types if needed)
                if (!processToSampleProject.ContainsKey(processId))
                {
                    processToSampleProject[processId] = project.Name;
                }
            }
        }

        // Cache the results
        _processCache = processes;
        _processProjectCountCache = processProjectCounts;
        _processToSampleProjectCache = processToSampleProject;
    }

    /// <summary>
    /// Clears the internal cache (useful for testing or force refresh)
    /// </summary>
    public void ClearCache()
    {
        _processCache = null;
        _processProjectCountCache = null;
        _processToSampleProjectCache = null;
    }
}

/// <summary>
/// Information about a process in Azure DevOps
/// </summary>
public class ProcessInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ReferenceName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsSystemProcess { get; set; }
}

/// <summary>
/// Information about a work item type
/// </summary>
public class WorkItemTypeInfo
{
    public string ReferenceName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsDisabled { get; set; }
}

/// <summary>
/// Process information combined with project count
/// </summary>
public class ProcessWithProjectCount
{
    public ProcessInfo Process { get; set; } = new();
    public int ProjectCount { get; set; }
}
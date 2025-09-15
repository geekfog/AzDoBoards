using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi;

namespace AzDoBoards.Client;

public class Process(ConnectionFactory connectionFactory) : Base(connectionFactory)
{
    private Dictionary<Guid, ProcessInfo>? _processCache;
    private Dictionary<Guid, string>? _processToSampleProjectCache;

    /// <summary>
    /// Gets all processes in the Azure DevOps organization using the Process API (cached)
    /// </summary>
    /// <returns>List of processes with their details including project counts</returns>
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
        if (_processCache == null)
            await LoadProcessDataAsync();

        return _processCache!.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ProjectCount);
    }

    /// <summary>
    /// Gets unique work item types for a specific process (highly optimized)
    /// </summary>
    /// <param name="processId">The ID of the process</param>
    /// <returns>List of unique work item types for the specified process</returns>
    public async Task<List<WorkItemTypeInfo>> GetWorkItemTypesForProcessAsync(Guid processId)
    {
        var connection = await _connectionFactory.GetConnectionAsync();

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
                IsSystemProcess = process.ParentProcessTypeId == Guid.Empty, // System processes don't have a parent
                ProjectCount = 0 // Will be updated below
            };
            processes[process.TypeId] = processInfo;
        }

        // Get project counts and sample projects
        var projectClient = connection.GetClient<ProjectHttpClient>();
        var projects = await projectClient.GetProjects();

        var processToSampleProject = new Dictionary<Guid, string>();

        foreach (var project in projects)
        {
            var projectDetail = await projectClient.GetProject(project.Id.ToString(), includeCapabilities: true);

            if (projectDetail.Capabilities?.TryGetValue("processTemplate", out var processTemplate) == true &&
                processTemplate.TryGetValue("templateTypeId", out var templateTypeId) &&
                Guid.TryParse(templateTypeId, out var processId))
            {
                // Update project count
                if (processes.TryGetValue(processId, out var processInfo))
                {
                    processInfo.ProjectCount++;
                }

                // Store first project name as sample (for getting work item types if needed)
                if (!processToSampleProject.ContainsKey(processId))
                {
                    processToSampleProject[processId] = project.Name;
                }
            }
        }

        // Cache the results
        _processCache = processes;
        _processToSampleProjectCache = processToSampleProject;
    }

    /// <summary>
    /// Clears the internal cache (useful for testing or force refresh)
    /// </summary>
    public void ClearCache()
    {
        _processCache = null;
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
    public int ProjectCount { get; set; }
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
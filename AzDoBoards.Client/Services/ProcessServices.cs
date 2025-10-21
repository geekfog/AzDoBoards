using AzDoBoards.Client.Models;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi;

namespace AzDoBoards.Client.Services;

public class ProcessServices(ConnectionFactory connectionFactory) : Base(connectionFactory)
{
    private Dictionary<Guid, ProcessSummary>? _processCache;
    private Dictionary<Guid, string>? _processToSampleProjectCache;

    /// <summary>
    /// Gets all processes in the Azure DevOps organization using the Process API (cached)
    /// </summary>
    /// <returns>List of processes with their details including project counts</returns>
    public async Task<List<ProcessSummary>> GetProcessesAsync()
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
    public async Task<List<WorkItemTypeSummary>> GetWorkItemTypesForProcessAsync(Guid processId)
    {
        var connection = await _connectionFactory.GetConnectionAsync();

        var processClient = connection.GetClient<WorkItemTrackingProcessHttpClient>();
        var workItemTypes = await processClient.GetProcessWorkItemTypesAsync(processId);

        return [.. workItemTypes.Select(wit => new WorkItemTypeSummary
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

        var processes = new Dictionary<Guid, ProcessSummary>();
        foreach (var process in allProcesses)
        {
            var processInfo = new ProcessSummary
            {
                Id = process.TypeId,
                Name = process.Name,
                Description = process.Description,
                ReferenceName = process.ReferenceName,
                IsDefault = process.IsDefault,
                IsEnabled = process.IsEnabled,
                IsSystemProcess = process.ParentProcessTypeId == Guid.Empty, // System processes don't have a parent
                ProjectCount = 0, // Will be updated below
                ParentProcessId = process.ParentProcessTypeId == Guid.Empty ? null : process.ParentProcessTypeId
            };
            processes[process.TypeId] = processInfo;
        }

        // Resolve parent process names
        foreach (var processInfo in processes.Values)
        {
            if (processInfo.ParentProcessId.HasValue && processes.TryGetValue(processInfo.ParentProcessId.Value, out var parentProcess))
            {
                processInfo.ParentProcessName = parentProcess.Name;
            }
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
    /// Gets field definitions and layout for a specific work item type
    /// </summary>
    /// <param name="processId">The ID of the process</param>
    /// <param name="workItemType">The work item type name (display name like "Feature" or "Epic")</param>
    /// <returns>Work item type layout with field definitions</returns>
    public async Task<WorkItemTypeLayout> GetWorkItemTypeLayoutAsync(Guid processId, string workItemType)
    {
        var connection = await _connectionFactory.GetConnectionAsync();
        var processClient = connection.GetClient<WorkItemTrackingProcessHttpClient>();

        // First, get all work item types to find the reference name
        var workItemTypes = await processClient.GetProcessWorkItemTypesAsync(processId);
        var workItemTypeInfo = workItemTypes.FirstOrDefault(wit => wit.Name.Equals(workItemType, StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidOperationException($"Work item type '{workItemType}' not found in process {processId}");

        // Use the reference name for API calls
        var workItemTypeRefName = workItemTypeInfo.ReferenceName;

        // Get all fields for the work item type using reference name (Note: GetAllWorkItemTypeFieldsAsync doesn't populate AllowedValues, so we query each field individually with ProcessWorkItemTypeFieldsExpandLevel.AllowedValues to get picklist values)
        var fields = await processClient.GetAllWorkItemTypeFieldsAsync(processId, workItemTypeRefName);

        // Get the layout (page structure with groups) using reference name
        var layout = await processClient.GetFormLayoutAsync(processId, workItemTypeRefName);

        var fieldGroups = new List<WorkItemFieldGroup>();
        var allFields = new List<WorkItemFieldDefinition>();

        // Map fields to our model
        foreach (var field in fields)
        {
            var fieldDef = new WorkItemFieldDefinition
            {
                ReferenceName = field.ReferenceName,
                Name = field.Name,
                Description = field.Description ?? string.Empty,
                FieldType = field.Type.ToString(),
                IsRequired = field.Required,
                IsReadOnly = field.ReadOnly,
                IsCore = field.ReferenceName.StartsWith("System."),
                DefaultValue = field.DefaultValue?.ToString(),
                AllowedValues = new List<string>()
            };

            // For non-readonly fields, get AllowedValues using the expand parameter
            if (!fieldDef.IsReadOnly)
            {
                try
                {
                    var fieldDetail = await processClient.GetWorkItemTypeFieldAsync(processId, workItemTypeRefName, field.ReferenceName, Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi.Models.ProcessWorkItemTypeFieldsExpandLevel.AllowedValues);
                    if (fieldDetail?.AllowedValues != null && fieldDetail.AllowedValues.Length > 0)
                    {
                        fieldDef.AllowedValues = fieldDetail.AllowedValues
                            .Where(v => v != null)
                            .Select(v => v.ToString() ?? string.Empty)
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();
                    }
                }
                catch
                {
                    // If we can't get field details, continue with empty AllowedValues
                }
            }

            allFields.Add(fieldDef);
        }

        // Parse layout to get field groups
        if (layout?.Pages?.Any() == true)
        {
            foreach (var page in layout.Pages)
            {
                if (page.Sections?.Any() == true)
                {
                    foreach (var section in page.Sections)
                    {
                        if (section.Groups?.Any() == true)
                        {
                            foreach (var group in section.Groups)
                            {
                                var fieldGroup = new WorkItemFieldGroup
                                {
                                    Id = group.Id,
                                    Label = group.Label,
                                    IsVisible = group.Visible ?? true
                                };

                                if (group.Controls?.Any() == true)
                                {
                                    foreach (var control in group.Controls)
                                    {
                                        // Find the field definition for this control
                                        var fieldDef = allFields.FirstOrDefault(f => 
                                            f.ReferenceName.Equals(control.Id, StringComparison.OrdinalIgnoreCase));
                                        
                                        if (fieldDef != null)
                                        {
                                            // Override with control-specific settings if available
                                            if (!string.IsNullOrEmpty(control.Label))
                                                fieldDef.Name = control.Label;
                                            
                                            if (control.ReadOnly == true)
                                                fieldDef.IsReadOnly = true;

                                            fieldGroup.Fields.Add(fieldDef);
                                        }
                                    }
                                }

                                if (fieldGroup.Fields.Any())
                                {
                                    fieldGroups.Add(fieldGroup);
                                }
                            }
                        }
                    }
                }
            }
        }

        return new WorkItemTypeLayout
        {
            WorkItemType = workItemType,
            Groups = fieldGroups,
            AllFields = allFields
        };
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

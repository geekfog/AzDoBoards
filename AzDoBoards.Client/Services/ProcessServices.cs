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
        var workItemClient = connection.GetClient<Microsoft.TeamFoundation.WorkItemTracking.WebApi.WorkItemTrackingHttpClient>();

        // First, get all work item types to find the reference name
        var workItemTypes = await processClient.GetProcessWorkItemTypesAsync(processId);
        var workItemTypeInfo = workItemTypes.FirstOrDefault(wit => 
            wit.Name.Equals(workItemType, StringComparison.OrdinalIgnoreCase));

        if (workItemTypeInfo == null)
        {
            throw new InvalidOperationException($"Work item type '{workItemType}' not found in process {processId}");
        }

        // Use the reference name for API calls
        var workItemTypeRefName = workItemTypeInfo.ReferenceName;

        // Get all fields for the work item type using reference name
        var fields = await processClient.GetAllWorkItemTypeFieldsAsync(processId, workItemTypeRefName);

        // Get field allowed values by querying organization-level picklists
        var fieldAllowedValues = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        
        try
        {
            Console.WriteLine($"[ProcessServices] Loading picklists from Azure DevOps");
            
            // Get all picklists at the organization level (not process-specific)
            var picklists = await processClient.GetListsMetadataAsync();
            
            Console.WriteLine($"[ProcessServices] Found {picklists.Count} picklists");
            
            foreach (var picklistMetadata in picklists)
            {
                try
                {
                    // Get the full picklist with items (use Guid.Empty since picklists are org-level)
                    var picklist = await processClient.GetListAsync(picklistMetadata.Id);
                    if (picklist?.Items != null && picklist.Items.Any())
                    {
                        var values = picklist.Items.ToList();
                        
                        // Map picklist to fields by name (picklist names often match field names)
                        // Common mappings:
                        var fieldMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "Priority", "Microsoft.VSTS.Common.Priority" },
                            { "Severity", "Microsoft.VSTS.Common.Severity" },
                            { "Risk", "Microsoft.VSTS.Common.Risk" },
                            { "Value Area", "Microsoft.VSTS.Common.ValueArea" },
                            { "ValueArea", "Microsoft.VSTS.Common.ValueArea" },
                            { "Activity", "Microsoft.VSTS.Common.Activity" }
                        };
                        
                        if (fieldMappings.TryGetValue(picklist.Name, out var fieldReferenceName))
                        {
                            fieldAllowedValues[fieldReferenceName] = values;
                            Console.WriteLine($"[ProcessServices] ✓ Loaded picklist '{picklist.Name}' with {values.Count} values");
                        }
                        else
                        {
                            Console.WriteLine($"[ProcessServices] ○ Found picklist '{picklist.Name}' but no field mapping");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ProcessServices] Could not load picklist {picklistMetadata.Name}: {ex.Message}");
                }
            }
            
            Console.WriteLine($"[ProcessServices] Successfully loaded allowed values for {fieldAllowedValues.Count} fields");
            
            // If no picklists were loaded, use fallback
            if (fieldAllowedValues.Count == 0)
            {
                throw new Exception("No picklists were successfully loaded");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProcessServices] Error loading picklists: {ex.Message}");
            Console.WriteLine($"[ProcessServices] Using hardcoded fallback values");
            
            fieldAllowedValues = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Microsoft.VSTS.Common.Priority", ["1", "2", "3", "4"] },
                { "Microsoft.VSTS.Common.Severity", ["1 - Critical", "2 - High", "3 - Medium", "4 - Low"] },
                { "Microsoft.VSTS.Common.Risk", ["1 - High", "2 - Medium", "3 - Low"] },
                { "Microsoft.VSTS.Common.ValueArea", ["Architectural", "Business"] },
                { "Microsoft.VSTS.Common.Activity", ["Deployment", "Design", "Development", "Documentation", "Requirements", "Testing"] }
            };
        }

        // Get the layout (page structure with groups) using reference name
        var layout = await processClient.GetFormLayoutAsync(processId, workItemTypeRefName);

        var fieldGroups = new List<WorkItemFieldGroup>();
        var allFields = new List<WorkItemFieldDefinition>();

        // Map fields to our model
        foreach (var field in fields)
        {
            var allowedValues = field.AllowedValues?.Select(v => v.ToString() ?? string.Empty).ToList() ?? new List<string>();
            
            // If no allowed values from field definition, try to get from Work Item Tracking API field definitions
            if (!allowedValues.Any() && fieldAllowedValues.TryGetValue(field.ReferenceName, out var trackedValues))
            {
                allowedValues = trackedValues;
            }
            
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
                AllowedValues = allowedValues
            };

            // Log fields with allowed values for debugging
            if (allowedValues.Any())
            {
                Console.WriteLine($"[ProcessServices] ✓ {field.Name}: {allowedValues.Count} values ({string.Join(", ", allowedValues.Take(3))}{(allowedValues.Count > 3 ? "..." : "")})");
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

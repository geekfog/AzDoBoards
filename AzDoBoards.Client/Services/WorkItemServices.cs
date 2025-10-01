using AzDoBoards.Client.Models;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;
using System.Text;
using WorkItem = AzDoBoards.Client.Models.WorkItem;

namespace AzDoBoards.Client.Services;

public class WorkItemServices(ConnectionFactory connectionFactory) : Base(connectionFactory)
{
    /// <summary>
    /// Gets work items based on the provided filter with dynamic WHERE clause building
    /// </summary>
    /// <param name="filter">Filter parameters for the work item query</param>
    /// <returns>List of work items matching the filter criteria</returns>
    public async Task<List<WorkItem>> GetWorkItemsAsync(WorkItemFilter filter)
    {
        var connection = await _connectionFactory.GetConnectionAsync();
        var workItemClient = connection.GetClient<WorkItemTrackingHttpClient>();

        // If state categories are specified, we need to resolve them to actual states
        var resolvedFilter = await ResolveStateCategoriesAsync(filter);
        
        var wiqlQuery = BuildWiqlQuery(resolvedFilter); // Build the WIQL query dynamically
        var wiql = new Wiql { Query = wiqlQuery };
        var workItemQueryResult = await workItemClient.QueryByWiqlAsync(wiql, resolvedFilter.ProjectId);

        if (workItemQueryResult.WorkItems?.Any() != true) return [];

        var workItemIds = workItemQueryResult.WorkItems.Select(wi => wi.Id).ToArray();
        var workItems = await workItemClient.GetWorkItemsAsync(workItemIds, expand: WorkItemExpand.All);
        return [.. workItems.Select(azWorkItem => ConvertToWorkItem(azWorkItem, null))];
    }

    /// <summary>
    /// Gets work items for the selected hierarchy level with Proposed and InProgress state categories
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="workItemTypes">List of work item types from the selected hierarchy level</param>
    /// <returns>List of work items in Proposed or InProgress state categories</returns>
    public async Task<List<WorkItem>> GetWorkItemsForHierarchyLevelAsync(string projectId, List<WorkItemTypeSummary> workItemTypes)
    {
        if (workItemTypes.Count == 0) return [];

        // Create a dictionary to map work item type names to their colors
        var workItemTypeColors = workItemTypes.ToDictionary(
            wit => wit.Name, 
            wit => wit.Color,
            StringComparer.OrdinalIgnoreCase
        );

        var filter = new WorkItemFilter
        {
            ProjectId = projectId,
            WorkItemTypes = workItemTypes.Select(wit => wit.Name).ToList(),
            StateCategories = new List<string> { "Proposed", "InProgress" },
            Top = 500 // Increase limit for hierarchy queries
        };

        var connection = await _connectionFactory.GetConnectionAsync();
        var workItemClient = connection.GetClient<WorkItemTrackingHttpClient>();

        // If state categories are specified, we need to resolve them to actual states
        var resolvedFilter = await ResolveStateCategoriesAsync(filter);
        
        var wiqlQuery = BuildWiqlQuery(resolvedFilter); // Build the WIQL query dynamically
        var wiql = new Wiql { Query = wiqlQuery };
        var workItemQueryResult = await workItemClient.QueryByWiqlAsync(wiql, resolvedFilter.ProjectId);

        if (workItemQueryResult.WorkItems?.Any() != true) return [];

        var workItemIds = workItemQueryResult.WorkItems.Select(wi => wi.Id).ToArray();
        var workItems = await workItemClient.GetWorkItemsAsync(workItemIds, expand: WorkItemExpand.All);
        return [.. workItems.Select(azWorkItem => ConvertToWorkItem(azWorkItem, workItemTypeColors))];
    }

    /// <summary>
    /// Resolves state categories to actual states for the given work item types
    /// </summary>
    /// <param name="filter">Original filter with state categories</param>
    /// <returns>Filter with resolved states</returns>
    private async Task<WorkItemFilter> ResolveStateCategoriesAsync(WorkItemFilter filter)
    {
        // If no state categories are specified, return the original filter
        if (filter.StateCategories.Count == 0)
            return filter;

        var connection = await _connectionFactory.GetConnectionAsync();
        var workItemClient = connection.GetClient<WorkItemTrackingHttpClient>();

        var resolvedStates = new HashSet<string>();

        // For each work item type, get its states and filter by category
        foreach (var workItemType in filter.WorkItemTypes)
        {
            try
            {
                // Query to get work item type definition
                var wiql = new Wiql 
                { 
                    Query = $"SELECT [System.State] FROM workitems WHERE [System.WorkItemType] = '{workItemType}' AND [System.TeamProject] = '{filter.ProjectId}'" 
                };
                
                var result = await workItemClient.QueryByWiqlAsync(wiql, filter.ProjectId);
                
                if (result.WorkItems?.Any() == true)
                {
                    // Get a sample work item to analyze its states
                    var sampleWorkItemIds = result.WorkItems.Take(10).Select(wi => wi.Id).ToArray();
                    var sampleWorkItems = await workItemClient.GetWorkItemsAsync(sampleWorkItemIds, expand: WorkItemExpand.Fields);
                    
                    foreach (var workItem in sampleWorkItems)
                    {
                        var state = GetFieldValue<string>(workItem, "System.State");
                        var stateCategory = DetermineStateCategory(state, workItemType);
                        
                        if (!string.IsNullOrEmpty(state) && filter.StateCategories.Contains(stateCategory))
                        {
                            resolvedStates.Add(state);
                        }
                    }
                }
                
                // Also add common states based on known patterns
                foreach (var stateCategory in filter.StateCategories)
                {
                    resolvedStates.UnionWith(GetCommonStatesForCategory(stateCategory, workItemType));
                }
            }
            catch
            {
                // If we can't resolve states for a work item type, fall back to common states
                foreach (var stateCategory in filter.StateCategories)
                {
                    resolvedStates.UnionWith(GetCommonStatesForCategory(stateCategory, workItemType));
                }
            }
        }

        // Create a new filter with resolved states
        var resolvedFilter = new WorkItemFilter
        {
            ProjectId = filter.ProjectId,
            WorkItemTypes = filter.WorkItemTypes,
            States = [.. resolvedStates],
            StateCategories = [], // Clear state categories as we've resolved them to states
            AssignedToFilter = filter.AssignedToFilter,
            IterationFilter = filter.IterationFilter,
            AreaFilter = filter.AreaFilter,
            ModifiedSince = filter.ModifiedSince,
            Top = filter.Top
        };

        return resolvedFilter;
    }

    /// <summary>
    /// Determines the state category for a given state and work item type
    /// </summary>
    /// <param name="state">Work item state</param>
    /// <param name="workItemType">Work item type</param>
    /// <returns>State category</returns>
    private static string DetermineStateCategory(string? state, string workItemType)
    {
        if (string.IsNullOrEmpty(state)) return "Unknown";

        var stateLower = state.ToLower();
        
        // Proposed states
        if (stateLower is "new" or "proposed" or "to do" or "open")
            return "Proposed";
            
        // InProgress states
        if (stateLower is "active" or "in progress" or "committed" or "doing" or "approved")
            return "InProgress";
            
        // Completed states
        if (stateLower is "done" or "closed" or "resolved" or "completed")
            return "Completed";
            
        // Removed states
        if (stateLower is "removed" or "rejected" or "abandoned")
            return "Removed";

        return "Unknown";
    }

    /// <summary>
    /// Gets common states for a given state category and work item type
    /// </summary>
    /// <param name="stateCategory">State category</param>
    /// <param name="workItemType">Work item type</param>
    /// <returns>List of common states</returns>
    private static List<string> GetCommonStatesForCategory(string stateCategory, string workItemType)
    {
        var workItemTypeLower = workItemType.ToLower();
        
        return stateCategory switch
        {
            "Proposed" => workItemTypeLower switch
            {
                "epic" or "feature" => ["New", "Proposed"],
                "user story" or "story" or "product backlog item" => ["New", "To Do"],
                "bug" => ["New", "Proposed"],
                "task" => ["New", "To Do"],
                _ => ["New", "Proposed", "To Do", "Open"]
            },
            "InProgress" => workItemTypeLower switch
            {
                "epic" or "feature" => ["Active", "In Progress"],
                "user story" or "story" or "product backlog item" => ["Active", "Committed", "In Progress"],
                "bug" => ["Active", "Approved"],
                "task" => ["Active", "In Progress", "Doing"],
                _ => ["Active", "In Progress", "Committed", "Approved", "Doing"]
            },
            "Completed" => ["Done", "Closed", "Resolved", "Completed"],
            "Removed" => ["Removed", "Rejected", "Abandoned"],
            _ => []
        };
    }

    /// <summary>
    /// Builds a dynamic WIQL query based on the filter parameters
    /// </summary>
    /// <param name="filter">Filter parameters</param>
    /// <returns>WIQL query string</returns>
    private static string BuildWiqlQuery(WorkItemFilter filter)
    {
        var query = new StringBuilder();
        query.AppendLine("SELECT [System.Id], [System.Title], [System.WorkItemType], [System.State],");
        query.AppendLine("       [System.AssignedTo], [Microsoft.VSTS.Common.Priority], [Microsoft.VSTS.Scheduling.TargetDate],");
        query.AppendLine("       [System.ChangedDate], [System.CreatedDate], [System.IterationPath], [System.AreaPath], [System.Tags]");
        query.AppendLine("FROM workitems");

        var whereConditions = new List<string>();

        if (filter.WorkItemTypes.Count != 0) // Add work item type filter
        {
            var workItemTypeCondition = string.Join("', '", filter.WorkItemTypes.Select(wit => wit.Replace("'", "''")));
            whereConditions.Add($"[System.WorkItemType] IN ('{workItemTypeCondition}')");
        }

        if (filter.States.Count != 0) // Add state filter (resolved from state categories)
        {
            var stateCondition = string.Join("', '", filter.States.Select(s => s.Replace("'", "''")));
            whereConditions.Add($"[System.State] IN ('{stateCondition}')");
        }

        if (!string.IsNullOrEmpty(filter.AssignedToFilter)) // Add assigned to filter
        {
            if (filter.AssignedToFilter.Equals("@Me", StringComparison.OrdinalIgnoreCase))
            {
                whereConditions.Add("[System.AssignedTo] = @Me");
            }
            else if (filter.AssignedToFilter.Equals("Unassigned", StringComparison.OrdinalIgnoreCase))
            {
                whereConditions.Add("[System.AssignedTo] = ''");
            }
            else
            {
                whereConditions.Add($"[System.AssignedTo] CONTAINS '{filter.AssignedToFilter.Replace("'", "''")}'");
            }
        }

        if (!string.IsNullOrEmpty(filter.IterationFilter)) // Add iteration filter
            whereConditions.Add($"[System.IterationPath] UNDER '{filter.IterationFilter.Replace("'", "''")}'");

        if (!string.IsNullOrEmpty(filter.AreaFilter)) // Add area filter
            whereConditions.Add($"[System.AreaPath] UNDER '{filter.AreaFilter.Replace("'", "''")}'");
        
        if (filter.ModifiedSince.HasValue) // Add modified since filter
            whereConditions.Add($"[System.ChangedDate] >= '{filter.ModifiedSince.Value:yyyy-MM-dd}'");

        if (whereConditions.Any()) // Add WHERE clause if there are conditions
            query.AppendLine($"WHERE {string.Join(" AND ", whereConditions)}");

        query.AppendLine("ORDER BY [System.ChangedDate] DESC");

        return query.ToString();
    }

    /// <summary>
    /// Converts Azure DevOps WorkItem to our WorkItem model
    /// </summary>
    /// <param name="azWorkItem">Azure DevOps work item</param>
    /// <param name="workItemTypeColors">Optional dictionary mapping work item types to their colors</param>
    /// <returns>Converted work item</returns>
    private static WorkItem ConvertToWorkItem(Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem azWorkItem, Dictionary<string, string>? workItemTypeColors = null)
    {
        var workItem = new WorkItem
        {
            Id = azWorkItem.Id ?? 0,
            Fields = azWorkItem.Fields?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>()
        };

        // Extract common fields with null safety
        workItem.Title = GetFieldValue<string>(azWorkItem, "System.Title") ?? string.Empty;
        workItem.WorkItemType = GetFieldValue<string>(azWorkItem, "System.WorkItemType") ?? string.Empty;
        workItem.State = GetFieldValue<string>(azWorkItem, "System.State") ?? string.Empty;
        workItem.Priority = GetFieldValue<string>(azWorkItem, "Microsoft.VSTS.Common.Priority") ?? string.Empty;
        workItem.IterationPath = GetFieldValue<string>(azWorkItem, "System.IterationPath") ?? string.Empty;
        workItem.AreaPath = GetFieldValue<string>(azWorkItem, "System.AreaPath") ?? string.Empty;

        // Determine state category from the state
        workItem.StateCategory = DetermineStateCategory(workItem.State, workItem.WorkItemType);

        // Handle assigned to field
        var assignedTo = GetFieldValue<IdentityRef>(azWorkItem, "System.AssignedTo");
        if (assignedTo != null)
        {
            workItem.AssignedToDisplayName = assignedTo.DisplayName ?? string.Empty;
            workItem.AssignedToEmail = assignedTo.UniqueName ?? string.Empty;
        }

        // Handle dates
        workItem.TargetDate = GetFieldValue<DateTime?>(azWorkItem, "Microsoft.VSTS.Scheduling.TargetDate");
        workItem.ModifiedDate = GetFieldValue<DateTime>(azWorkItem, "System.ChangedDate");
        workItem.CreatedDate = GetFieldValue<DateTime>(azWorkItem, "System.CreatedDate");

        // Handle tags
        var tagsString = GetFieldValue<string>(azWorkItem, "System.Tags");
        if (!string.IsNullOrEmpty(tagsString))
        {
            workItem.Tags = tagsString.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                     .Select(tag => tag.Trim())
                                     .ToList();
        }

        // Set color based on work item type - use provided colors if available, otherwise use a neutral fallback
        if (workItemTypeColors?.TryGetValue(workItem.WorkItemType, out var configuredColor) == true && !string.IsNullOrEmpty(configuredColor))
        {
            workItem.Color = configuredColor;
        }
        else
        {
            // Use a neutral fallback color when no configured color is available
            workItem.Color = "#6C757D"; // Bootstrap's secondary color - neutral gray
        }

        return workItem;
    }

    /// <summary>
    /// Safely gets a field value from the work item
    /// </summary>
    /// <typeparam name="T">Type to cast the field value to</typeparam>
    /// <param name="workItem">Work item</param>
    /// <param name="fieldName">Field name</param>
    /// <returns>Field value or default</returns>
    private static T? GetFieldValue<T>(Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem workItem, string fieldName)
    {
        if (workItem.Fields?.TryGetValue(fieldName, out var value) == true && value != null)
        {
            try
            {
                if (typeof(T) == typeof(DateTime) && value is string dateString)
                    return DateTime.TryParse(dateString, out var dateValue) ? (T)(object)dateValue : default;

                if (typeof(T) == typeof(DateTime?) && value is string nullableDateString)
                    return DateTime.TryParse(nullableDateString, out var dateValue) ? (T)(object)dateValue : default;

                return (T)value;
            }
            catch
            {
                return default;
            }
        }
        return default;
    }

    /// <summary>
    /// Gets parent-child relationships for work items from Azure DevOps
    /// </summary>
    /// <param name="workItemIds">List of work item IDs to get relationships for</param>
    /// <returns>Dictionary mapping parent work item ID to list of child work item IDs</returns>
    public async Task<Dictionary<int, List<int>>> GetWorkItemRelationshipsAsync(List<int> workItemIds)
    {
        var parentChildMap = new Dictionary<int, List<int>>();
        
        try
        {
            var connection = await _connectionFactory.GetConnectionAsync();
            var workItemClient = connection.GetClient<WorkItemTrackingHttpClient>();

            // Get work items with relations expanded
            var workItems = await workItemClient.GetWorkItemsAsync(
                workItemIds, 
                expand: WorkItemExpand.Relations);

            foreach (var workItem in workItems)
            {
                if (workItem.Relations?.Any() == true)
                {
                    // Look for child relationships
                    var children = new List<int>();
                    
                    foreach (var relation in workItem.Relations)
                    {
                        // Azure DevOps uses different relation types:
                        // - "System.LinkTypes.Hierarchy-Forward" for parent->child
                        // - "System.LinkTypes.Hierarchy-Reverse" for child->parent
                        if (relation.Rel == "System.LinkTypes.Hierarchy-Forward" && 
                            relation.Url != null)
                        {
                            // Extract work item ID from the URL
                            var urlParts = relation.Url.Split('/');
                            if (urlParts.Length > 0 && int.TryParse(urlParts[^1], out var childId))
                            {
                                children.Add(childId);
                            }
                        }
                    }
                    
                    if (children.Any())
                    {
                        parentChildMap[workItem.Id ?? 0] = children;
                    }
                }
            }

            return parentChildMap;
        }
        catch (Exception ex)
        {
            // Log error but don't throw - let the caller handle the empty result
            System.Diagnostics.Debug.WriteLine($"Error getting work item relationships: {ex.Message}");
            return parentChildMap;
        }
    }
}

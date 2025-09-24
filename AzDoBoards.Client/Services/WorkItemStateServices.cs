using AzDoBoards.Client.Models;
using Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi.Models;

namespace AzDoBoards.Client.Services;

public class WorkItemStateServices(ConnectionFactory connectionFactory) : Base(connectionFactory)
{
    /// <summary>
    /// Gets work item states for a specific process, grouped by state category
    /// </summary>
    /// <param name="processId">The ID of the process</param>
    /// <returns>List of work item state groups ordered by workflow</returns>
    public async Task<List<WorkItemStateGroup>> GetWorkItemStatesForProcessAsync(Guid processId)
    {
        var connection = await _connectionFactory.GetConnectionAsync();
        var processClient = connection.GetClient<WorkItemTrackingProcessHttpClient>();

        // Get all work item types for the process
        var workItemTypes = await processClient.GetProcessWorkItemTypesAsync(processId);
        
        // Dictionary to collect states by category
        var statesByCategory = new Dictionary<string, Dictionary<string, WorkItemStateSummary>>();

        // Process each work item type to collect its states
        foreach (var workItemType in workItemTypes)
        {
            try
            {
                var states = await processClient.GetStateDefinitionsAsync(processId, workItemType.ReferenceName);
                
                foreach (var state in states)
                {
                    var category = state.StateCategory;
                    var stateName = state.Name;

                    // Initialize category if not exists
                    if (!statesByCategory.ContainsKey(category))
                    {
                        statesByCategory[category] = new Dictionary<string, WorkItemStateSummary>();
                    }

                    // Add or update state
                    if (!statesByCategory[category].ContainsKey(stateName))
                    {
                        statesByCategory[category][stateName] = new WorkItemStateSummary
                        {
                            Name = stateName,
                            Color = state.Color ?? "#1976d2",
                            Category = category,
                            Order = state.Order,
                            IsCompleted = category == "Completed",
                            WorkItemTypes = new List<string>()
                        };
                    }

                    // Add work item type to the state
                    if (!statesByCategory[category][stateName].WorkItemTypes.Contains(workItemType.Name))
                    {
                        statesByCategory[category][stateName].WorkItemTypes.Add(workItemType.Name);
                    }
                }
            }
            catch
            {
                // Skip work item types that don't have accessible state definitions
                continue;
            }
        }

        // Convert to grouped format with proper ordering
        var stateGroups = new List<WorkItemStateGroup>();
        
        // Define the order and display names for state categories
        var categoryOrder = new Dictionary<string, (string DisplayName, int Order)>
        {
            { "Proposed", ("Proposed", 1) },
            { "InProgress", ("In Progress", 2) },
            { "Completed", ("Completed", 3) },
            { "Removed", ("Removed", 4) }
        };

        foreach (var categoryKvp in statesByCategory.OrderBy(kvp => 
            categoryOrder.ContainsKey(kvp.Key) ? categoryOrder[kvp.Key].Order : 999))
        {
            var category = categoryKvp.Key;
            var states = categoryKvp.Value.Values.OrderBy(s => s.Order).ToList();

            var displayName = categoryOrder.ContainsKey(category) 
                ? categoryOrder[category].DisplayName 
                : category;

            var order = categoryOrder.ContainsKey(category) 
                ? categoryOrder[category].Order 
                : 999;

            stateGroups.Add(new WorkItemStateGroup
            {
                Category = category,
                DisplayName = displayName,
                States = states,
                Order = order
            });
        }

        return stateGroups.OrderBy(g => g.Order).ToList();
    }
}
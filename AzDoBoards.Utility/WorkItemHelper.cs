using System.Text.Json;
using AzDoBoards.Utility.Models;

namespace AzDoBoards.Utility;

/// <summary>
/// Helper class for work item hierarchy operations
/// </summary>
public static class WorkItemHelper
{
    /// <summary>
    /// Gets the color for a specific hierarchy level
    /// </summary>
    /// <param name="level">Zero-based level index</param>
    /// <returns>Hex color string</returns>
    public static string GetLevelColor(int level)
    {
        var colors = new[] { "#339947", "#FF7B00", "#773B93", "#009CCC", "#F2CB1D", "#F599D1", "#E60017" };
        return colors[level % colors.Length];
    }

    /// <summary>
    /// Gets the SVG icon for a work item type based on its name
    /// </summary>
    /// <param name="workItemTypeName">The name of the work item type</param>
    /// <returns>SVG icon string</returns>
    public static string GetWorkItemTypeSvgIcon(string workItemTypeName)
    {
        return workItemTypeName.ToLower() switch
        {
            "initiative" => IconSvg.WorkItemIconInitiative,
            "epic" => IconSvg.WorkItemIconEpic,
            "feature" => IconSvg.WorkItemIconFeature,
            "user story" => IconSvg.WorkItemIconUserStory,
            "story" => IconSvg.WorkItemIconUserStory,
            "bug" => IconSvg.WorkItemIconBug,
            "task" => IconSvg.WorkItemIconTask,
            "issue" => IconSvg.WorkItemIconIssue,
            "impediment" => IconSvg.WorkItemIconIssue,
            "research" => IconSvg.WorkItemIconResearch,
            "test case" => IconSvg.WorkItemIconTestCase,
            "test plan" => IconSvg.WorkItemIconTestPlan,
            "test suite" => IconSvg.WorkItemIconTestSuite,
            "product backlog item" => IconSvg.WorkItemIconProductBacklogItem,
            "requirement" => IconSvg.WorkItemIconRequirement,
            "change request" => IconSvg.WorkItemIconChangeRequest,
            "review" => IconSvg.WorkItemIconReview,
            "risk" => IconSvg.WorkItemIconRisk,
            _ => IconSvg.WorkItemIconProductBacklogItem
        };
    }

    /// <summary>
    /// Parses hierarchy JSON string into a structured format (legacy format for backward compatibility)
    /// </summary>
    /// <param name="hierarchyJson">JSON string representing the hierarchy</param>
    /// <returns>Array of string arrays representing levels and work item names</returns>
    public static string[][]? ParseHierarchyJson(string hierarchyJson)
    {
        if (string.IsNullOrEmpty(hierarchyJson) || hierarchyJson == "[]")
            return null;

        try
        {
            // Try parsing as new format first
            var newFormat = JsonSerializer.Deserialize<HierarchyLevel[]>(hierarchyJson);
            if (newFormat != null)
            {
                return newFormat.Select(level => level.WorkItemTypes.ToArray()).ToArray();
            }
        }
        catch
        {
            // Fall back to legacy format
        }

        try
        {
            return JsonSerializer.Deserialize<string[][]>(hierarchyJson);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses hierarchy JSON string into the new HierarchyLevel format
    /// </summary>
    /// <param name="hierarchyJson">JSON string representing the hierarchy</param>
    /// <returns>Array of HierarchyLevel objects</returns>
    public static HierarchyLevel[]? ParseHierarchyLevelsJson(string hierarchyJson)
    {
        if (string.IsNullOrEmpty(hierarchyJson) || hierarchyJson == "[]")
            return null;

        try
        {
            // Try parsing as new format first
            var newFormat = JsonSerializer.Deserialize<HierarchyLevel[]>(hierarchyJson);
            if (newFormat != null)
            {
                return newFormat;
            }
        }
        catch
        {
            // Fall back to legacy format and convert
            try
            {
                var legacyFormat = JsonSerializer.Deserialize<string[][]>(hierarchyJson);
                if (legacyFormat != null)
                {
                    return legacyFormat.Select(level => new HierarchyLevel
                    {
                        WorkItemTypes = level.ToList(),
                        Audience = new List<string>() // Empty audiences for legacy data
                    }).ToArray();
                }
            }
            catch
            {
                // Ignore
            }
        }

        return null;
    }

    /// <summary>
    /// Serializes hierarchy structure to JSON (legacy format)
    /// </summary>
    /// <param name="hierarchy">Hierarchy structure</param>
    /// <returns>JSON string</returns>
    public static string SerializeHierarchy(IEnumerable<IEnumerable<string>> hierarchy)
    {
        var hierarchyData = hierarchy.Select(level => level.ToArray()).ToArray();
        return JsonSerializer.Serialize(hierarchyData);
    }

    /// <summary>
    /// Serializes hierarchy levels to the new JSON format
    /// </summary>
    /// <param name="hierarchyLevels">Hierarchy levels with audiences</param>
    /// <returns>JSON string</returns>
    public static string SerializeHierarchyLevels(IEnumerable<HierarchyLevel> hierarchyLevels)
    {
        return JsonSerializer.Serialize(hierarchyLevels.ToArray());
    }
}
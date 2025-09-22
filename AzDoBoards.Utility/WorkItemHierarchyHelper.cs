using System.Text.Json;

namespace AzDoBoards.Utility;

/// <summary>
/// Helper class for work item hierarchy operations
/// </summary>
public static class WorkItemHierarchyHelper
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
    /// Gets the icon for a work item type based on its name
    /// </summary>
    /// <param name="workItemTypeName">The name of the work item type</param>
    /// <returns>Material icon string</returns>
    public static string GetWorkItemTypeIcon(string workItemTypeName)
    {
        return workItemTypeName.ToLower() switch
        {
            "initiative" => "account_balance",
            "epic" => "workspace_premium",
            "feature" => "emoji_events",
            "user story" => "auto_stories",
            "story" => "bookmark_border",
            "bug" => "bug_report",
            "task" => "check_box",
            "test case" => "biotech",
            "test plan" => "assignment",
            "test suite" => "folder",
            "issue" => "construction",
            "research" => "science",
            "investigation" => "gavel",
            _ => "work"
        };
    }

    /// <summary>
    /// Gets the display icon for a hierarchy level
    /// </summary>
    /// <param name="level">Zero-based level index</param>
    /// <returns>Material icon string</returns>
    public static string GetLevelIcon(int level)
    {
        var icons = new[]
        {
            "looks_one", "looks_two", "looks_3",
            "looks_4", "looks_5", "looks_6"
        };
        return level < icons.Length ? icons[level] : "looks_one";
    }

    /// <summary>
    /// Parses hierarchy JSON string into a structured format
    /// </summary>
    /// <param name="hierarchyJson">JSON string representing the hierarchy</param>
    /// <returns>Array of string arrays representing levels and work item names</returns>
    public static string[][]? ParseHierarchyJson(string hierarchyJson)
    {
        if (string.IsNullOrEmpty(hierarchyJson) || hierarchyJson == "[]")
            return null;

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
    /// Serializes hierarchy structure to JSON
    /// </summary>
    /// <param name="hierarchy">Hierarchy structure</param>
    /// <returns>JSON string</returns>
    public static string SerializeHierarchy(IEnumerable<IEnumerable<string>> hierarchy)
    {
        var hierarchyData = hierarchy.Select(level => level.ToArray()).ToArray();
        return JsonSerializer.Serialize(hierarchyData);
    }
}
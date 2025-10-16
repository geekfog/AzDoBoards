namespace AzDoBoards.Client.Models;

/// <summary>
/// Represents a field definition for a work item type
/// </summary>
public class WorkItemFieldDefinition
{
    public string ReferenceName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsCore { get; set; }
    public string? DefaultValue { get; set; }
    public List<string> AllowedValues { get; set; } = new();
    public string? HelpText { get; set; }
}

/// <summary>
/// Represents a group of fields in the work item layout
/// </summary>
public class WorkItemFieldGroup
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public List<WorkItemFieldDefinition> Fields { get; set; } = new();
}

/// <summary>
/// Represents the complete layout configuration for a work item type
/// </summary>
public class WorkItemTypeLayout
{
    public string WorkItemType { get; set; } = string.Empty;
    public List<WorkItemFieldGroup> Groups { get; set; } = new();
    public List<WorkItemFieldDefinition> AllFields { get; set; } = new();
}

namespace AzDoBoards.Models.Roadmap;

/// <summary>
/// Configuration for roadmap display
/// </summary>
public class Config
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TimeUnit TimeUnit { get; set; } = TimeUnit.Week;
    public int ZoomLevel { get; set; } = 1;
    public bool ShowDependencies { get; set; } = true;
    public bool ShowRelated { get; set; } = true;
    public List<string> VisibleWorkItemTypes { get; set; } = new();
}

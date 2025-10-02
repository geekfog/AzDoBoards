using Microsoft.JSInterop;
using AzDoBoards.Utility.Models;

namespace AzDoBoards.Ui.Services;

/// <summary>
/// Service for handling roadmap drag and drop operations with minimal JavaScript interop
/// </summary>
public class RoadmapDragDropService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<RoadmapDragDropService> _logger;

    public RoadmapDragDropService(IJSRuntime jsRuntime, ILogger<RoadmapDragDropService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>
    /// Calculate target date based on mouse position over timeline (only called on drop)
    /// </summary>
    public async Task<DateTime?> CalculateTargetDateAsync(double clientX, DateTime startDate, DateTime endDate, string? timelineElementId = null)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<DateCalculationResult?>(
                "roadmapDateCalculator.calculateTargetDate",
                clientX, startDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), 
                endDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), timelineElementId);

            if (result?.Date != null && DateTime.TryParse(result.Date, out var targetDate))
            {
                _logger.LogDebug("Calculated target date: {TargetDate} at position {Position}%", 
                    targetDate, Math.Round(result.Percentage * 100, 1));
                return targetDate;
            }

            _logger.LogWarning("Failed to calculate target date from client position {ClientX}", clientX);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating target date");
            return null;
        }
    }

    /// <summary>
    /// Validate if drop position is within timeline bounds
    /// </summary>
    public async Task<bool> IsValidDropPositionAsync(double clientX, double clientY, string? timelineElementId = null)
    {
        try
        {
            var bounds = await _jsRuntime.InvokeAsync<TimelineBounds?>(
                "roadmapDateCalculator.getTimelineBounds", timelineElementId);

            if (bounds == null) return false;

            return clientX >= bounds.Left && clientX <= bounds.Right && 
                   clientY >= bounds.Top && clientY <= bounds.Bottom;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating drop position");
            return false;
        }
    }

    /// <summary>
    /// Process drag and drop with comprehensive error handling
    /// </summary>
    public async Task<DragDropResult> ProcessDropAsync(
        UnscheduledWorkItem draggedItem, 
        double clientX, 
        double clientY,
        DateTime timelineStart, 
        DateTime timelineEnd,
        int? targetEpicId = null)
    {
        try
        {
            // Validate drop position
            var isValidPosition = await IsValidDropPositionAsync(clientX, clientY);
            if (!isValidPosition)
            {
                return new DragDropResult
                {
                    Success = false,
                    ErrorMessage = "Invalid drop position - must drop within timeline area",
                    SuggestedAction = "Try dropping the item directly on the timeline calendar"
                };
            }

            // Calculate target date
            var targetDate = await CalculateTargetDateAsync(clientX, timelineStart, timelineEnd);
            if (!targetDate.HasValue)
            {
                return new DragDropResult
                {
                    Success = false,
                    ErrorMessage = "Could not determine target date from drop position",
                    SuggestedAction = "Try dropping closer to a specific date on the timeline"
                };
            }

            // Validate business rules
            if (targetDate < DateTime.Today.AddDays(-30))
            {
                return new DragDropResult
                {
                    Success = false,
                    ErrorMessage = $"Target date {targetDate:MMM dd, yyyy} is too far in the past",
                    SuggestedAction = "Choose a more recent date"
                };
            }

            return new DragDropResult
            {
                Success = true,
                TargetDate = targetDate.Value,
                TargetEpicId = targetEpicId,
                Message = $"Ready to schedule {draggedItem.Title} for {targetDate:MMM dd, yyyy}" + 
                         (targetEpicId.HasValue ? $" in epic {targetEpicId}" : " as unassigned")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing drag and drop for item {ItemId}", draggedItem.WorkItemId);
            return new DragDropResult
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred during drop processing",
                SuggestedAction = "Please try again or contact support if the issue persists"
            };
        }
    }
}

/// <summary>
/// Result of date calculation from JavaScript
/// </summary>
public class DateCalculationResult
{
    public string? Date { get; set; }
    public double Percentage { get; set; }
    public double Position { get; set; }
}

/// <summary>
/// Timeline bounds from JavaScript
/// </summary>
public class TimelineBounds
{
    public double Left { get; set; }
    public double Right { get; set; }
    public double Width { get; set; }
    public double Top { get; set; }
    public double Bottom { get; set; }
}

/// <summary>
/// Result of drag and drop operation
/// </summary>
public class DragDropResult
{
    public bool Success { get; set; }
    public DateTime TargetDate { get; set; }
    public int? TargetEpicId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;
}
# Roadmap Drag & Drop Implementation

## Overview
This implementation adds performant drag and drop functionality to the Roadmap page, allowing users to drag work items from the unscheduled panel to the timeline. The date is shown dynamically during the drag operation using JavaScript for performance, while the actual drop processing is handled by C#.

## Architecture

### JavaScript Layer (Performance-Critical)
**File:** `AzDoBoards.Ui/Components/Pages/Roadmap.razor.js`

Handles all visual feedback during drag operations:
- **Visual Indicator**: A green vertical line follows the mouse cursor across the timeline
- **Date Label**: Shows the target date that would be assigned if dropped at current position
- **Real-time Updates**: Updates occur on every `dragover` event without server round-trips
- **Smooth Performance**: Uses native DOM manipulation for 60fps feedback

Key Functions:
- `initialize()`: Sets up the drag system with timeline configuration
- `updateConfig()`: Updates when timeline zoom or date range changes
- `handleDragOver()`: Shows indicator and calculates target date
- `handleDrop()`: Hides indicator (actual processing done by C#)
- `dispose()`: Cleans up event listeners and DOM elements

### C# Layer (Business Logic)
**File:** `AzDoBoards.Ui/Components/Pages/Roadmap.razor`

Handles the actual data updates:
- **State Management**: Tracks `draggedWorkItemId` and `dropTargetDate`
- **API Calls**: Updates work items via `RoadmapService`
- **Data Refresh**: Reloads roadmap data after successful updates
- **Error Handling**: Shows user-friendly messages on failures

Key Methods:
- `OnDragStart()`: Stores the dragged work item ID
- `HandleTimelineDrop()`: Processes the drop and updates the work item
- `HandleUnscheduleDrop()`: Handles dropping back to unscheduled
- `SetDropTargetDate()`: JavaScript callback to set the target date

## User Experience Flow

1. **Start Drag**: User drags an unscheduled work item
2. **Hover Timeline**: As mouse moves over timeline:
   - Green indicator line appears at mouse position
   - Date label shows calculated target date
   - Updates smoothly without lag
3. **Drop**: User releases mouse button
   - JavaScript hides the indicator
   - C# processes the drop
   - Work item is updated in Azure DevOps
   - Roadmap refreshes to show new schedule
4. **Feedback**: Success/error message shown to user

## Configuration Updates

The JavaScript module automatically syncs when users change:
- **Time Unit**: Week/Month/Quarter view
- **Date Range**: Start and end dates
- **Pixels per Day**: Calculated based on zoom level

This is handled by calling `UpdateJavaScriptConfig()` after configuration changes.

## Performance Benefits

### Without JavaScript (Previous):
- Each mouse move required server round-trip
- UI updates delayed by network latency
- Choppy visual feedback
- Increased server load

### With JavaScript (Current):
- All visual feedback handled client-side
- Smooth 60fps indicator movement
- No server calls until actual drop
- Minimal server load

## Error Handling

- **JavaScript Initialization Failure**: Logs error, drag still works but without visual feedback
- **Drop Processing Failure**: Shows error snackbar, data remains unchanged
- **Network Issues**: Error messages guide user to retry
- **Invalid Dates**: Clamped to timeline boundaries automatically

## Browser Compatibility

Tested and working on:
- Chrome/Edge (Chromium)
- Firefox
- Safari

Uses standard HTML5 Drag and Drop API with no special polyfills required.

## Future Enhancements

Potential improvements:
1. Show work item preview while dragging
2. Multi-select drag and drop
3. Drag to resize (change duration)
4. Snap to week/month boundaries
5. Collision detection with existing items
6. Undo/redo support

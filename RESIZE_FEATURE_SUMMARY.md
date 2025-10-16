# Timeline Work Item Resize Feature

## Overview
Added the ability to resize work items on the roadmap timeline by dragging the edges to change start and target dates independently.

## Features Implemented

### 1. Visual Resize Handles
- **Left Edge (::before pseudo-element)**: Changes the start date while keeping the target date fixed
- **Right Edge (::after pseudo-element)**: Changes the target date while keeping the start date fixed
- Resize handles appear on hover with a semi-transparent gradient and border
- Active resize operations show a green highlight for visual feedback

### 2. Resize Behavior

#### Left Edge Resize (Start Date)
- Dragging the left edge adjusts the start date
- Target date remains unchanged
- Minimum duration of ~1 day (20px) enforced
- Start date cannot be after target date (validation with warning message)

#### Right Edge Resize (Target Date)
- Dragging the right edge adjusts the target date
- Start date remains unchanged
- If the work item had a calculated start date (90 days before target), it becomes explicit
- Target date cannot be before start date (validation with warning message)
- Minimum duration of ~1 day (20px) enforced

### 3. Date Validation Rules
- Start date must always be before target date
- Target date must always be after start date
- If either validation fails, the operation is reverted with a warning message
- User-friendly error messages displayed via Snackbar

### 4. Visual Feedback During Resize
- Drag indicator line shows the new position
- Date label displays:
  - "Start Date" when resizing left edge
  - "Target Date" when resizing right edge
- Work item element updates in real-time during drag
- Smooth transitions and visual states (`.resizing-left`, `.resizing-right`)

### 5. Edge Detection
- 8-pixel threshold from edges for resize handle activation
- Precise edge detection prevents accidental resizes
- Cursor changes to `ew-resize` when hovering over resize handles

## Technical Implementation

### Files Modified

#### 1. `Roadmap.razor.css`
Added CSS for resize handles and visual feedback:
```css
- Pseudo-elements (::before and ::after) for resize handles
- Hover states with gradient backgrounds
- Active resize visual states
- Cursor changes for resize operations
```

#### 2. `Roadmap.razor.js`
Added JavaScript resize functionality:
```javascript
- Mouse event handlers (mousedown, mousemove, mouseup)
- Resize mode tracking ('left', 'right', null)
- Real-time position calculations
- Date calculations from pixel positions
- Work item ID extraction from elements
- Blazor interop for resize completion
```

**New Variables:**
- `resizeMode`: Tracks which edge is being resized
- `resizingElement`: Reference to the element being resized
- `resizeStartX`, `resizeOriginalLeft`, `resizeOriginalWidth`: Position tracking
- `resizeWorkItemId`: ID of the work item being resized

**New Functions:**
- `handleMouseDown()`: Detects resize operations on edges
- `handleMouseMove()`: Updates position during resize
- `handleMouseUp()`: Completes resize and calls Blazor
- `getWorkItemIdFromElement()`: Extracts work item ID from element

#### 3. `Roadmap.razor`
Added C# handlers for resize operations:
```csharp
- [JSInvokable] HandleResizeLeft(int workItemId, string newStartDateString)
- [JSInvokable] HandleResizeRight(int workItemId, string newTargetDateString)
- UpdateTimelineItemStartDate(int workItemId, DateTime? newStartDate)
- UpdateTimelineItemTargetDate(int workItemId, DateTime? newTargetDate)
```

**Key Features:**
- Immediate local state updates for responsiveness
- Date validation before API calls
- Automatic reversion on API failure
- Proper handling of calculated vs. explicit start dates
- Comprehensive logging for debugging

## User Experience

### How to Use
1. **Hover** over a timeline work item to reveal resize handles on both edges
2. **Click and drag** the left edge to change the start date
3. **Click and drag** the right edge to change the target date
4. **Release** to commit the change
5. Visual feedback shows the new date during the drag operation
6. Success/error messages appear via Snackbar notifications

### Visual Cues
- Resize handles: Semi-transparent white gradient with borders
- Active resize: Green highlight on the active handle
- Drag indicator: Vertical green line with date label
- Cursor: Changes to horizontal resize cursor (?) when hovering over edges

### Error Handling
- Invalid date ranges trigger warnings and revert changes
- API failures automatically reload the roadmap data
- User-friendly error messages explain what went wrong

## Integration with Existing Features
- Works seamlessly with existing drag-and-drop functionality
- Drag-and-drop is disabled during resize operations
- Resize operations respect timeline boundaries
- Compatible with all zoom levels (Week, Month, Quarter)
- Works with collapsed/expanded swimlanes

## Testing Recommendations
1. Test resizing work items at different zoom levels
2. Verify date validation (start before target, target after start)
3. Test edge cases (minimum duration, timeline boundaries)
4. Test with calculated vs. explicit start dates
5. Verify proper reversion on API failures
6. Test interaction with drag-and-drop operations
7. Test with different screen sizes and browser zoom levels

## Bug Fixes Applied
### Issue 1: Date not showing on indicator during resize
**Problem:** The date label wasn't displaying the actual date when resizing.
**Solution:** 
- Added explicit ID (`roadmap-drag-date-subtitle`) to the subtitle element
- Changed from using `querySelector('div:last-child')` to `querySelector('#roadmap-drag-date-subtitle')`
- Ensured proper initialization of the date text element

### Issue 2: Target date resize changing start date
**Problem:** When resizing the right edge (target date), the start date was being recalculated and updated.
**Solution:**
- Removed the logic that calculated and set a start date when it was previously calculated
- Changed `HandleResizeRight` to ONLY update the target date
- The start date now remains unchanged when resizing the right edge
- Only validates that target date is after start date if a start date exists

## Future Enhancements
- Add keyboard shortcuts (Shift+Drag for fixed duration, Ctrl+Drag for both dates)
- Add snap-to-grid functionality for specific time intervals
- Show duration in days during resize
- Add undo/redo functionality
- Support multi-select resize

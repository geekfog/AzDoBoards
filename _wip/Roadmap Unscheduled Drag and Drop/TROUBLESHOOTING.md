# Troubleshooting: Roadmap Drag & Drop Issues

## Issue: No visual feedback when dragging

### Check Browser Console
Open browser console (F12) and look for:

1. **Module initialization messages:**
   ```
   Roadmap drag module initializing...
   Timeline container found: <div>
   Drag indicator created and appended to timeline container
   Roadmap drag module initialized successfully
   ```

2. **Common errors:**
   - `Timeline container not found!` - The DOM isn't ready yet
   - `Error calling SetDropTargetDate` - .NET interop issue
   - Module import errors - JavaScript file not accessible

### Solutions:

#### If timeline container not found:
1. Check that `.timeline-items-container` element exists in the DOM
2. Verify the page has fully loaded with data (not in loading state)
3. Try refreshing the page

#### If module won't load:
1. Clear browser cache (Ctrl+Shift+Del)
2. Check the file exists at: `wwwroot/Components/Pages/Roadmap.razor.js`
3. Verify the file is included in build output
4. Check browser network tab for 404 errors

## Issue: Work item not updating after drop

### Check Logs
Look in the application console/logs for:

```
Drag started for work item {id}
Drop target date set to: {date}
HandleTimelineDrop called. DraggedItemId: {id}, DropTargetDate: {date}
Updating work item {id} with target date {date}
Successfully updated work item {id}
```

### Common Problems:

#### Drop target date is null:
- **Symptom:** Log shows `Drop called but no drop target date set`
- **Cause:** JavaScript isn't calling `SetDropTargetDate` successfully
- **Fix:** 
  1. Check browser console for JavaScript errors
  2. Verify `dotNetHelper.invokeMethodAsync('SetDropTargetDate', ...)` is called
  3. Check the date string format is valid

#### Dragged work item ID is null:
- **Symptom:** Log shows `Drop called but no dragged work item ID`
- **Cause:** `OnDragStart` wasn't called or state was cleared
- **Fix:**
  1. Verify the unscheduled item has `draggable="true"`
  2. Check `@ondragstart` event is wired up
  3. Ensure drag started from correct element

#### API call fails:
- **Symptom:** `Failed to update work item {id}`
- **Causes:**
  1. Network connectivity issue
  2. Authentication expired
  3. Insufficient permissions
  4. Work item locked by another user
- **Fix:**
  1. Check network tab for API response
  2. Re-authenticate if needed
  3. Verify user has edit permissions
  4. Wait and retry

## Issue: Date indicator shows wrong date

### Verify Configuration
Check these values in browser console:

```javascript
// In handleDragOver, add temporary logging:
console.log('Mouse X:', x);
console.log('Pixels per day:', pixelsPerDay);
console.log('Timeline start:', timelineStartDate);
console.log('Calculated date:', targetDate);
```

### Common Issues:

#### Wrong pixels per day:
- Week view should be 10
- Month view should be 3
- Quarter view should be 1

**Fix:** Verify `GetPixelsPerDay()` in C# code matches JavaScript config

#### Wrong start date:
- Should be aligned based on time unit
- Week: Use actual start date
- Month: First day of month
- Quarter: First day of quarter

**Fix:** Verify `GetAlignedStartDate()` is passed to JavaScript

#### Wrong date calculation:
The formula is: `targetDate = startDate + (mouseX / pixelsPerDay) days`

## Testing Checklist

Use this to systematically test the feature:

### 1. Visual Feedback Test
- [ ] Drag an item from unscheduled panel
- [ ] Green indicator line appears on timeline
- [ ] Date label shows above the line
- [ ] Date label updates as you move mouse
- [ ] Indicator disappears when dropping or canceling

### 2. Drop Processing Test
- [ ] Drop item on timeline
- [ ] Loading indicator appears briefly
- [ ] Item disappears from unscheduled panel
- [ ] Item appears on timeline at correct date
- [ ] Success message appears

### 3. Date Accuracy Test
- [ ] Drop at different positions
- [ ] Verify dates match indicator labels
- [ ] Check Azure DevOps work item directly
- [ ] Dates should match exactly

### 4. Edge Cases Test
- [ ] Drop at far left edge
- [ ] Drop at far right edge
- [ ] Drop outside timeline (should do nothing)
- [ ] Cancel drag (ESC key)
- [ ] Drag already-scheduled item

## Browser DevTools Commands

Run these in browser console to diagnose:

```javascript
// Check if module loaded
console.log(window.roadmapModule); // Should be undefined (module is scoped)

// Check if timeline container exists
console.log(document.querySelector('.timeline-items-container'));

// Check if drag indicator exists
console.log(document.getElementById('roadmap-drag-indicator'));

// Force show indicator (for testing)
const indicator = document.getElementById('roadmap-drag-indicator');
if (indicator) {
    indicator.style.opacity = '1';
    indicator.style.left = '100px';
}
```

## Common Solutions Summary

### No visual feedback:
1. Check browser console for errors
2. Verify timeline container exists
3. Try page refresh
4. Clear browser cache

### Drop not working:
1. Check application logs
2. Verify authentication
3. Check network tab for API errors
4. Verify work item permissions

### Wrong dates:
1. Verify timeline configuration
2. Check aligned start date calculation
3. Verify pixels per day matches view mode

## Getting More Help

When reporting issues, provide:

1. **Browser Console Logs** (all messages)
2. **Application Logs** (from OnInitializedAsync through drop)
3. **Network Tab** (showing API calls)
4. **Screenshots** of the issue
5. **Timeline configuration** (dates, time unit, pixels per day)
6. **Work item ID** being dragged
7. **Expected vs actual behavior**

## Reset Instructions

If things are completely broken:

1. **Hard Refresh:** Ctrl+Shift+R (Chrome) or Cmd+Shift+R (Mac)
2. **Clear Site Data:**
   - F12 > Application > Clear Storage
   - Check all boxes
   - Click "Clear site data"
3. **Restart Application**
4. **Navigate to Roadmap page**
5. **Try drag and drop again**

If still not working, check:
- Is JavaScript file being served? (Network tab)
- Are there any console errors at all?
- Can you drag ANY element on the page?
- Does the page render correctly otherwise?

# Quick Reference: Drag & Drop Debugging

## Console Commands (Run in Browser)

### Check if module loaded:
```javascript
// Should see initialization messages
// Look for: "Roadmap drag module initialized successfully"
```

### Check if timeline container exists:
```javascript
document.querySelector('.timeline-items-container')
// Should return: <div class="timeline-items-container">
```

### Check if drag indicator exists:
```javascript
document.getElementById('roadmap-drag-indicator')
// Should return: <div id="roadmap-drag-indicator">
```

### Manually show indicator (for testing):
```javascript
const indicator = document.getElementById('roadmap-drag-indicator');
if (indicator) {
    indicator.style.opacity = '1';
    indicator.style.left = '200px';
    const label = indicator.querySelector('#roadmap-drag-date-label');
    if (label) label.textContent = 'Test Date';
}
```

## Expected Log Sequence

### On Page Load:
```
Roadmap drag module initializing...
Timeline date range: {start: ..., end: ...}
Timeline container found: <div>
Drag indicator created
Roadmap drag module initialized successfully
```

### On Drag Start:
```
Drag started for work item {id}
```

### While Dragging:
```
Drop target date set to: {date}
(repeats as mouse moves)
```

### On Drop:
```
Drop detected on timeline
HandleTimelineDrop called. DraggedItemId: {id}, DropTargetDate: {date}
Updating work item {id} with target date {date}
Successfully updated work item {id}
Drag ended
```

## Quick Diagnostics

### Visual feedback not showing?
1. ? Check console for errors
2. ? Verify timeline container exists
3. ? Try page refresh
4. ? Run `/roadmap-diagnostics`

### Drop not working?
1. ? Check if `dropTargetDate` is set
2. ? Check application logs
3. ? Verify network request succeeds
4. ? Check authentication

### Wrong dates?
1. ? Verify aligned start date
2. ? Check pixels per day value
3. ? Verify time unit setting

## Key Files

- **Main Component:** `Roadmap.razor`
- **JavaScript Module:** `Roadmap.razor.js`
- **Diagnostics:** `/roadmap-diagnostics`
- **Full Guide:** `TROUBLESHOOTING.md`

## Emergency Reset

1. Ctrl+Shift+R (hard refresh)
2. F12 > Application > Clear Site Data
3. Restart browser
4. Navigate to roadmap

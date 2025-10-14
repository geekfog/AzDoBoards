# Testing Guide: Roadmap Drag & Drop Feature

## Prerequisites
1. Have the application running
2. Be authenticated with Azure DevOps
3. Have at least one project configured with work items
4. Have some unscheduled work items in the Roadmap view

## Test Scenarios

### 1. Basic Drag & Drop to Timeline
**Steps:**
1. Navigate to `/roadmap` page
2. Locate an unscheduled work item in the left panel
3. Click and hold on the work item
4. Drag the item over the timeline area
5. **Expected:** A green vertical line should appear following your cursor
6. **Expected:** A date label should appear above the line showing the target date
7. Release the mouse button to drop
8. **Expected:** Work item is removed from unscheduled and appears on timeline
9. **Expected:** Success message appears: "Work item {ID} scheduled successfully"

### 2. Date Indicator Accuracy
**Steps:**
1. Start dragging an unscheduled item
2. Move slowly across the timeline
3. **Expected:** The date label updates smoothly without lag
4. **Expected:** Date corresponds to the position on the timeline
5. **Expected:** In Week view, dates change daily
6. **Expected:** In Month view, dates align with weeks
7. **Expected:** In Quarter view, dates align with months

### 3. Visual Feedback Performance
**Steps:**
1. Start dragging an item
2. Move rapidly across the timeline
3. **Expected:** Indicator line follows cursor smoothly (60fps)
4. **Expected:** No lag or stuttering
5. **Expected:** Date label remains readable while moving

### 4. Drag Back to Unscheduled
**Steps:**
1. Drag a scheduled item from the timeline
2. Drop it in the "Unscheduled Items" panel
3. **Expected:** Item is removed from timeline
4. **Expected:** Item appears in unscheduled list
5. **Expected:** Success message: "Work item {ID} unscheduled successfully"
6. **Expected:** Target date is cleared in Azure DevOps

### 5. Timeline Configuration Changes
**Steps:**
1. Start with Week view
2. Switch to Month view
3. **Expected:** Timeline redraws correctly
4. **Expected:** Drag functionality still works
5. Switch to Quarter view
6. **Expected:** Drag functionality still works
7. Change start/end dates
8. **Expected:** Drag functionality adapts to new date range

### 6. Drag Multiple Items
**Steps:**
1. Drag and drop first item
2. Immediately drag and drop second item
3. Repeat for 5-10 items quickly
4. **Expected:** All items are scheduled correctly
5. **Expected:** No data loss or corruption
6. **Expected:** Timeline updates after each drop

### 7. Error Handling
**Steps:**
1. Disconnect network (simulate offline)
2. Try to drag and drop an item
3. **Expected:** Error message appears
4. **Expected:** Item returns to unscheduled panel
5. Reconnect network
6. Retry the operation
7. **Expected:** Drop succeeds normally

### 8. Timeline Boundaries
**Steps:**
1. Drag an item to the far left edge of timeline
2. **Expected:** Date clamped to timeline start date
3. Drag an item to the far right edge
4. **Expected:** Date clamped to timeline end date
5. Drop at boundaries
6. **Expected:** Item scheduled with boundary dates

### 9. Browser Compatibility
**Test in each browser:**
- Chrome/Edge (Chromium)
- Firefox
- Safari

**For each:**
1. Verify drag indicator appears
2. Verify date label is readable
3. Verify smooth performance
4. Verify drop processing works

### 10. Concurrent User Testing
**Steps:**
1. Open roadmap in two different browsers/tabs
2. In first tab, drag and schedule an item
3. In second tab, click refresh button
4. **Expected:** Second tab shows the updated schedule
5. In second tab, drag and schedule a different item
6. In first tab, click refresh
7. **Expected:** First tab shows both items scheduled

## Performance Benchmarks

### Expected Performance Metrics:
- **Drag Indicator Response:** < 16ms (60fps)
- **Date Calculation:** < 1ms
- **Drop Processing:** < 2 seconds (including API call)
- **Timeline Refresh:** < 3 seconds
- **Memory Leak Test:** Run for 30 minutes with continuous dragging, no memory increase

## Known Limitations

1. **Single Item Drag:** Can only drag one item at a time
2. **No Undo:** Cannot undo a drop without manual edit
3. **No Collision Detection:** Can overlap items on timeline
4. **No Duration Editing:** Cannot drag edges to change duration

## Troubleshooting

### Drag indicator not appearing:
1. Check browser console for JavaScript errors
2. Verify `Roadmap.razor.js` file is accessible
3. Check that timeline container element exists
4. Verify JavaScript module initialized successfully

### Date label shows wrong date:
1. Check `GetPixelsPerDay()` calculation
2. Verify timeline start/end dates are correct
3. Check browser timezone settings

### Drop not processing:
1. Check network connectivity
2. Verify Azure DevOps authentication
3. Check server logs for API errors
4. Verify work item permissions

### Performance issues:
1. Check browser performance tab
2. Verify no console errors
3. Test in different browser
4. Check if antivirus/firewall interfering

## Reporting Issues

When reporting bugs, include:
1. Browser name and version
2. Steps to reproduce
3. Screenshot or video of issue
4. Browser console errors (F12 > Console)
5. Network tab showing API calls (F12 > Network)
6. Work item IDs involved

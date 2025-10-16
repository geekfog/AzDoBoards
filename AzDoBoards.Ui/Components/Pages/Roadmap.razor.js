/**
 * Roadmap drag and drop JavaScript module
 * Provides performant visual feedback during drag operations
 */

let dragIndicator = null;
let timelineContainer = null;
let timelineStartDate = null;
let timelineEndDate = null;
let pixelsPerDay = 1;
let dotNetHelper = null;
let draggedItemEndOffset = 0; // Offset from mouse to item's END position
let resizeMode = null; // 'left', 'right', or null
let resizingElement = null;
let resizeStartX = 0;
let resizeOriginalLeft = 0;
let resizeOriginalWidth = 0;
let resizeWorkItemId = 0;

/**
 * Initialize the drag and drop system
 */
export function initialize(dotNetRef, startDate, endDate, pxPerDay) {
    console.log('Roadmap drag module initializing...', { startDate, endDate, pxPerDay });
    
    dotNetHelper = dotNetRef;
    timelineStartDate = new Date(startDate);
    timelineEndDate = new Date(endDate);
    pixelsPerDay = pxPerDay;
    
    console.log('Timeline date range:', { start: timelineStartDate, end: timelineEndDate });
    
    // Find timeline container
    timelineContainer = document.querySelector('.timeline-items-container');
    
    if (!timelineContainer) {
        console.error('Timeline container not found! Looking for .timeline-items-container');
        return;
    }
    
    console.log('Timeline container found:', timelineContainer);
    
    // Create drag indicator overlay
    createDragIndicator();
    
    // Attach event listeners to timeline container
    timelineContainer.addEventListener('mousedown', handleMouseDown, true); // For resize
    timelineContainer.addEventListener('dragstart', handleDragStart, true); // Capture phase
    timelineContainer.addEventListener('dragover', handleDragOver);
    timelineContainer.addEventListener('dragleave', handleDragLeave);
    timelineContainer.addEventListener('drop', handleDrop);
    
    // Add global dragend listener to catch ESC from anywhere (unscheduled panel or timeline)
    document.addEventListener('dragend', handleDragEnd, true);
    
    // Add global mouse listeners for resize
    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
    
    console.log('Roadmap drag module initialized successfully');
}

/**
 * Update timeline configuration (called when zoom/dates change)
 */
export function updateConfig(startDate, endDate, pxPerDay) {
    console.log('Updating drag module config:', { startDate, endDate, pxPerDay });
    
    timelineStartDate = new Date(startDate);
    timelineEndDate = new Date(endDate);
    pixelsPerDay = pxPerDay;
    
    console.log('Config updated. Timeline date range:', { start: timelineStartDate, end: timelineEndDate });
}

/**
 * Clean up event listeners and DOM elements
 */
export function dispose() {
    console.log('Disposing drag module...');
    
    if (timelineContainer) {
        timelineContainer.removeEventListener('mousedown', handleMouseDown, true);
        timelineContainer.removeEventListener('dragstart', handleDragStart, true);
        timelineContainer.removeEventListener('dragover', handleDragOver);
        timelineContainer.removeEventListener('dragleave', handleDragLeave);
        timelineContainer.removeEventListener('drop', handleDrop);
    }
    
    // Remove global dragend listener
    document.removeEventListener('dragend', handleDragEnd, true);
    
    // Remove global mouse listeners
    document.removeEventListener('mousemove', handleMouseMove);
    document.removeEventListener('mouseup', handleMouseUp);
    
    if (dragIndicator && dragIndicator.parentNode) {
        dragIndicator.parentNode.removeChild(dragIndicator);
    }
    
    dragIndicator = null;
    timelineContainer = null;
    dotNetHelper = null;
    draggedItemEndOffset = 0;
    
    console.log('Drag module disposed');
}

/**
 * Create the visual drag indicator
 */
function createDragIndicator() {
    // Remove existing indicator if present
    const existing = document.getElementById('roadmap-drag-indicator');
    if (existing) {
        existing.remove();
    }
    
    dragIndicator = document.createElement('div');
    dragIndicator.id = 'roadmap-drag-indicator';
    dragIndicator.style.cssText = `
        position: absolute;
        top: 0;
        bottom: 0;
        width: 3px;
        background-color: #4CAF50;
        pointer-events: none;
        opacity: 0;
        transition: opacity 0.15s ease;
        z-index: 1000;
        box-shadow: 0 0 10px rgba(76, 175, 80, 0.8);
    `;
    
    // Add date label container
    const dateLabel = document.createElement('div');
    dateLabel.id = 'roadmap-drag-date-label';
    dateLabel.style.cssText = `
        position: absolute;
        top: 10px;
        left: 50%;
        transform: translateX(-50%);
        background-color: #4CAF50;
        color: white;
        padding: 8px 14px;
        border-radius: 6px;
        font-size: 13px;
        font-weight: 600;
        white-space: nowrap;
        box-shadow: 0 3px 10px rgba(0, 0, 0, 0.3);
        font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
        text-align: center;
    `;
    
    // Add the date text (will be updated during drag)
    const dateText = document.createElement('div');
    dateText.id = 'roadmap-drag-date-text';
    dateText.textContent = ''; // Initialize empty
    dateLabel.appendChild(dateText);
    
    // Add "Target Date" subtitle
    const subtitle = document.createElement('div');
    subtitle.id = 'roadmap-drag-date-subtitle';
    subtitle.textContent = 'Target Date';
    subtitle.style.cssText = `
        font-size: 10px;
        opacity: 0.9;
        margin-top: 2px;
        font-weight: 500;
    `;
    dateLabel.appendChild(subtitle);
    
    dragIndicator.appendChild(dateLabel);
    
    // Append to timeline container instead of body
    timelineContainer.appendChild(dragIndicator);
    
    console.log('Drag indicator created with Target Date label');
}

/**
 * Handle mousedown to detect resize operations
 */
function handleMouseDown(e) {
    const element = e.target;
    
    // Check if clicking on a timeline item
    if (!element.classList.contains('timeline-item')) {
        return;
    }
    
    const rect = element.getBoundingClientRect();
    const clickX = e.clientX;
    const edgeThreshold = 8; // pixels from edge to consider as resize handle
    
    // Check if clicking near the left edge (resize start date)
    if (clickX - rect.left <= edgeThreshold) {
        e.preventDefault();
        e.stopPropagation();
        
        resizeMode = 'left';
        resizingElement = element;
        resizeStartX = clickX;
        resizeOriginalLeft = parseFloat(element.style.left) || 0;
        resizeOriginalWidth = parseFloat(element.style.width) || rect.width;
        
        // Extract work item ID from the element
        resizeWorkItemId = getWorkItemIdFromElement(element);
        
        // Prevent drag start
        element.setAttribute('draggable', 'false');
        element.classList.add('resizing-left');
        
        console.log('Started resizing left edge', { workItemId: resizeWorkItemId, originalLeft: resizeOriginalLeft, originalWidth: resizeOriginalWidth });
        return;
    }
    
    // Check if clicking near the right edge (resize target date)
    if (rect.right - clickX <= edgeThreshold) {
        e.preventDefault();
        e.stopPropagation();
        
        resizeMode = 'right';
        resizingElement = element;
        resizeStartX = clickX;
        resizeOriginalLeft = parseFloat(element.style.left) || 0;
        resizeOriginalWidth = parseFloat(element.style.width) || rect.width;
        
        // Extract work item ID from the element
        resizeWorkItemId = getWorkItemIdFromElement(element);
        
        // Prevent drag start
        element.setAttribute('draggable', 'false');
        element.classList.add('resizing-right');
        
        console.log('Started resizing right edge', { workItemId: resizeWorkItemId, originalLeft: resizeOriginalLeft, originalWidth: resizeOriginalWidth });
        return;
    }
}

/**
 * Handle mouse move during resize
 */
function handleMouseMove(e) {
    if (!resizeMode || !resizingElement) {
        return;
    }
    
    e.preventDefault();
    
    const deltaX = e.clientX - resizeStartX;
    const scrollLeft = timelineContainer.scrollLeft;
    
    if (resizeMode === 'left') {
        // Resizing left edge (changing start date)
        const newLeft = resizeOriginalLeft + deltaX;
        const newWidth = resizeOriginalWidth - deltaX;
        
        // Minimum width of 20px (approximately 1 day for most zoom levels)
        if (newWidth >= 20) {
            resizingElement.style.left = `${newLeft}px`;
            resizingElement.style.width = `${newWidth}px`;
            
            // Show indicator at new start position
            if (dragIndicator) {
                dragIndicator.style.left = `${newLeft}px`;
                dragIndicator.style.opacity = '1';
                
                const startDate = calculateDateFromPosition(newLeft);
                if (startDate) {
                    const dateText = dragIndicator.querySelector('#roadmap-drag-date-text');
                    const subtitle = dragIndicator.querySelector('#roadmap-drag-date-subtitle');
                    if (dateText) {
                        dateText.textContent = formatDate(startDate);
                    }
                    if (subtitle) {
                        subtitle.textContent = 'Start Date';
                    }
                }
            }
        }
    } else if (resizeMode === 'right') {
        // Resizing right edge (changing target date)
        const newWidth = resizeOriginalWidth + deltaX;
        
        // Minimum width of 20px
        if (newWidth >= 20) {
            resizingElement.style.width = `${newWidth}px`;
            
            // Show indicator at new end position
            if (dragIndicator) {
                const newRight = resizeOriginalLeft + newWidth;
                dragIndicator.style.left = `${newRight}px`;
                dragIndicator.style.opacity = '1';
                
                const targetDate = calculateDateFromPosition(newRight);
                if (targetDate) {
                    const dateText = dragIndicator.querySelector('#roadmap-drag-date-text');
                    const subtitle = dragIndicator.querySelector('#roadmap-drag-date-subtitle');
                    if (dateText) {
                        dateText.textContent = formatDate(targetDate);
                    }
                    if (subtitle) {
                        subtitle.textContent = 'Target Date';
                    }
                }
            }
        }
    }
}

/**
 * Handle mouse up to complete resize
 */
function handleMouseUp(e) {
    if (!resizeMode || !resizingElement) {
        return;
    }
    
    console.log('Resize completed', { mode: resizeMode, workItemId: resizeWorkItemId });
    
    // Calculate the new dates based on final position
    const finalLeft = parseFloat(resizingElement.style.left) || 0;
    const finalWidth = parseFloat(resizingElement.style.width) || 0;
    const finalRight = finalLeft + finalWidth;
    
    const startDate = calculateDateFromPosition(finalLeft);
    const targetDate = calculateDateFromPosition(finalRight);
    
    console.log('New dates calculated', { 
        startDate: startDate?.toISOString(), 
        targetDate: targetDate?.toISOString(),
        mode: resizeMode 
    });
    
    // Notify Blazor of the resize operation
    if (dotNetHelper && startDate && targetDate && resizeWorkItemId > 0) {
        try {
            if (resizeMode === 'left') {
                // Changed start date, keep target date
                dotNetHelper.invokeMethodAsync('HandleResizeLeft', resizeWorkItemId, startDate.toISOString());
            } else if (resizeMode === 'right') {
                // Changed target date, keep start date
                dotNetHelper.invokeMethodAsync('HandleResizeRight', resizeWorkItemId, targetDate.toISOString());
            }
        } catch (error) {
            console.error('Error calling resize handler:', error);
        }
    }
    
    // Clean up
    resizingElement.setAttribute('draggable', 'true');
    resizingElement.classList.remove('resizing-left', 'resizing-right');
    
    if (dragIndicator) {
        dragIndicator.style.opacity = '0';
    }
    
    resizeMode = null;
    resizingElement = null;
    resizeStartX = 0;
    resizeOriginalLeft = 0;
    resizeOriginalWidth = 0;
    resizeWorkItemId = 0;
}

/**
 * Extract work item ID from timeline element
 */
function getWorkItemIdFromElement(element) {
    // Try to find the work item ID from the element's text content or data attributes
    const text = element.textContent || '';
    const match = text.match(/^(\d+)\s*-/);
    if (match) {
        return parseInt(match[1], 10);
    }
    return 0;
}

/**
 * Handle drag start to calculate offset from mouse to item's END
 */
function handleDragStart(e) {
    // If we're in resize mode, prevent drag
    if (resizeMode) {
        e.preventDefault();
        return;
    }
    const element = e.target;
    
    // Check if this is a timeline item (existing scheduled item)
    if (element.classList.contains('timeline-item')) {
        // Get the item's position and width
        const rect = element.getBoundingClientRect();
        const containerRect = timelineContainer.getBoundingClientRect();
        const scrollLeft = timelineContainer.scrollLeft;
        
        // Calculate where the mouse clicked relative to the timeline container
        const mouseX = e.clientX - containerRect.left + scrollLeft;
        
        // Calculate where the item's RIGHT EDGE is (this is the current target date position)
        const itemLeft = rect.left - containerRect.left + scrollLeft;
        const itemWidth = rect.width;
        const itemEnd = itemLeft + itemWidth;
        
        // Store the offset: how far is the mouse from the item's END?
        // Positive value = mouse is left of the end
        draggedItemEndOffset = itemEnd - mouseX;
        
        console.log('Dragging timeline item:', {
            mouseX,
            itemEnd,
            offset: draggedItemEndOffset,
            itemWidth
        });
    } else {
        // Unscheduled item - no offset needed, indicator at mouse position
        draggedItemEndOffset = 0;
        console.log('Dragging unscheduled item - no offset');
    }
}

/**
 * Handle drag over timeline
 * The indicator position represents where the work item will END (target date)
 */
function handleDragOver(e) {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    
    if (!dragIndicator || !timelineContainer) {
        console.warn('Drag over but indicator or container missing');
        return;
    }
    
    // Get mouse position relative to timeline container
    const rect = timelineContainer.getBoundingClientRect();
    const scrollLeft = timelineContainer.scrollLeft;
    const mouseX = e.clientX - rect.left + scrollLeft;
    
    // Calculate where the item's END will be
    // For existing items: mouse position + offset to end
    // For new items: mouse position (offset is 0)
    const targetEndX = mouseX + draggedItemEndOffset;
    
    // The indicator position represents the TARGET DATE (end of work item)
    const targetDate = calculateDateFromPosition(targetEndX);
    
    if (targetDate) {
        // Position the indicator at the target END position
        dragIndicator.style.left = `${targetEndX}px`;
        dragIndicator.style.opacity = '1';
        
        // Update date text and subtitle
        const dateText = dragIndicator.querySelector('#roadmap-drag-date-text');
        const subtitle = dragIndicator.querySelector('#roadmap-drag-date-subtitle');
        if (dateText) {
            dateText.textContent = formatDate(targetDate);
        }
        if (subtitle) {
            subtitle.textContent = 'Target Date';
        }
        
        // Notify Blazor of the target date (for drop processing)
        if (dotNetHelper) {
            try {
                dotNetHelper.invokeMethodAsync('SetDropTargetDate', targetDate.toISOString());
            } catch (error) {
                console.error('Error calling SetDropTargetDate:', error);
            }
        }
    }
}

/**
 * Handle drag leave timeline
 */
function handleDragLeave(e) {
    // Only hide if we're actually leaving the timeline container
    if (e.target === timelineContainer && !timelineContainer.contains(e.relatedTarget)) {
        if (dragIndicator) {
            dragIndicator.style.opacity = '0';
        }
    }
}

/**
 * Handle drop on timeline
 */
function handleDrop(e) {
    e.preventDefault();
    
    console.log('Drop detected on timeline');
    
    hideIndicator();
    
    // The actual drop processing is handled by Blazor's @ondrop event
}

/**
 * Handle drag end (including ESC key cancel) - global listener
 */
function handleDragEnd(e) {
    console.log('Drag ended (dropped or cancelled) from:', e.target.className);
    
    hideIndicator();
}

/**
 * Hide the indicator and reset state
 */
function hideIndicator() {
    if (dragIndicator) {
        dragIndicator.style.opacity = '0';
    }
    
    // Reset offset for next drag
    draggedItemEndOffset = 0;
}

/**
 * Calculate date from pixel position
 */
function calculateDateFromPosition(x) {
    if (!timelineStartDate || !timelineEndDate || pixelsPerDay <= 0) {
        console.warn('Cannot calculate date: missing timeline configuration');
        return null;
    }
    
    const daysFromStart = x / pixelsPerDay;
    const targetDate = new Date(timelineStartDate);
    targetDate.setDate(targetDate.getDate() + Math.round(daysFromStart));
    
    // Clamp to timeline bounds
    if (targetDate < timelineStartDate) {
        return new Date(timelineStartDate);
    }
    if (targetDate > timelineEndDate) {
        return new Date(timelineEndDate);
    }
    
    return targetDate;
}

/**
 * Calculate pixel position from date
 */
function calculatePositionFromDate(date) {
    if (!timelineStartDate || pixelsPerDay <= 0) {
        return 0;
    }
    
    const daysDiff = Math.floor((date - timelineStartDate) / (1000 * 60 * 60 * 24));
    return daysDiff * pixelsPerDay;
}

/**
 * Format date for display
 */
function formatDate(date) {
    const options = { 
        month: 'short', 
        day: 'numeric', 
        year: 'numeric' 
    };
    return date.toLocaleDateString('en-US', options);
}

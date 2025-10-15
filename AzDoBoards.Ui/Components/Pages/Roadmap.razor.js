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
    timelineContainer.addEventListener('dragstart', handleDragStart, true); // Capture phase
    timelineContainer.addEventListener('dragover', handleDragOver);
    timelineContainer.addEventListener('dragleave', handleDragLeave);
    timelineContainer.addEventListener('drop', handleDrop);
    
    // Add global dragend listener to catch ESC from anywhere (unscheduled panel or timeline)
    document.addEventListener('dragend', handleDragEnd, true);
    
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
        timelineContainer.removeEventListener('dragstart', handleDragStart, true);
        timelineContainer.removeEventListener('dragover', handleDragOver);
        timelineContainer.removeEventListener('dragleave', handleDragLeave);
        timelineContainer.removeEventListener('drop', handleDrop);
    }
    
    // Remove global dragend listener
    document.removeEventListener('dragend', handleDragEnd, true);
    
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
    dateLabel.appendChild(dateText);
    
    // Add "Target Date" subtitle
    const subtitle = document.createElement('div');
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
 * Handle drag start to calculate offset from mouse to item's END
 */
function handleDragStart(e) {
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
        
        // Update date text
        const dateText = dragIndicator.querySelector('#roadmap-drag-date-text');
        if (dateText) {
            dateText.textContent = formatDate(targetDate);
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

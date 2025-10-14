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
    
    // Attach event listeners
    timelineContainer.addEventListener('dragover', handleDragOver);
    timelineContainer.addEventListener('dragleave', handleDragLeave);
    timelineContainer.addEventListener('drop', handleDrop);
    
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
        timelineContainer.removeEventListener('dragover', handleDragOver);
        timelineContainer.removeEventListener('dragleave', handleDragLeave);
        timelineContainer.removeEventListener('drop', handleDrop);
    }
    
    if (dragIndicator && dragIndicator.parentNode) {
        dragIndicator.parentNode.removeChild(dragIndicator);
    }
    
    dragIndicator = null;
    timelineContainer = null;
    dotNetHelper = null;
    
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
    
    // Add date label
    const dateLabel = document.createElement('div');
    dateLabel.id = 'roadmap-drag-date-label';
    dateLabel.style.cssText = `
        position: absolute;
        top: 10px;
        left: 50%;
        transform: translateX(-50%);
        background-color: #4CAF50;
        color: white;
        padding: 6px 14px;
        border-radius: 6px;
        font-size: 13px;
        font-weight: 600;
        white-space: nowrap;
        box-shadow: 0 3px 10px rgba(0, 0, 0, 0.3);
        font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
    `;
    dragIndicator.appendChild(dateLabel);
    
    // Append to timeline container instead of body
    timelineContainer.appendChild(dragIndicator);
    
    console.log('Drag indicator created and appended to timeline container');
}

/**
 * Handle drag over timeline
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
    const x = e.clientX - rect.left + scrollLeft;
    
    // Calculate the target date based on mouse position
    const targetDate = calculateDateFromPosition(x);
    
    if (targetDate) {
        // Position the indicator relative to container
        dragIndicator.style.left = `${x}px`;
        dragIndicator.style.opacity = '1';
        
        // Update date label
        const dateLabel = dragIndicator.querySelector('#roadmap-drag-date-label');
        if (dateLabel) {
            dateLabel.textContent = formatDate(targetDate);
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
    
    if (dragIndicator) {
        dragIndicator.style.opacity = '0';
    }
    
    // The actual drop processing is handled by Blazor's @ondrop event
    // We just hide the indicator here
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

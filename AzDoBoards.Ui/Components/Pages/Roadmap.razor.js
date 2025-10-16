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
let contextMenu = null;
let contextMenuVisible = false;

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
    
    // Create context menu
    createContextMenu();
    
    // Add context menu listeners
    document.addEventListener('contextmenu', handleContextMenu);
    document.addEventListener('click', hideContextMenu);
    
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
    
    // Remove context menu listeners
    document.removeEventListener('contextmenu', handleContextMenu);
    document.removeEventListener('click', hideContextMenu);
    
    if (dragIndicator && dragIndicator.parentNode) {
        dragIndicator.parentNode.removeChild(dragIndicator);
    }
    
    if (contextMenu && contextMenu.parentNode) {
        contextMenu.parentNode.removeChild(contextMenu);
    }
    
    dragIndicator = null;
    timelineContainer = null;
    contextMenu = null;
    contextMenuVisible = false;
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
    // Match work item ID at the beginning of text (handles both "123 - Title" and "123-Title" formats)
    const match = text.match(/^\s*(\d+)\s*[-–]/);
    if (match) {
        return parseInt(match[1], 10);
    }
    
    // Fallback: try to find any number at the start
    const numberMatch = text.match(/^\s*(\d+)/);
    if (numberMatch) {
        return parseInt(numberMatch[1], 10);
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

/**
 * Create the context menu element
 */
function createContextMenu() {
    const existing = document.getElementById('roadmap-context-menu');
    if (existing) {
        existing.remove();
    }
    
    contextMenu = document.createElement('div');
    contextMenu.id = 'roadmap-context-menu';
    contextMenu.className = 'context-menu';
    
    const isDarkMode = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    const bgColor = isDarkMode ? '#1e1e1e' : '#ffffff';
    const textColor = isDarkMode ? '#e0e0e0' : '#333333';
    const borderColor = isDarkMode ? '#404040' : '#cccccc';
    const hoverBg = isDarkMode ? '#2a2a2a' : '#f5f5f5';
    
    contextMenu.style.cssText = `
        position: fixed;
        background: ${bgColor};
        border: 1px solid ${borderColor};
        border-radius: 4px;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.5);
        z-index: 999999;
        min-width: 160px;
        padding: 4px 0;
        opacity: 0;
        transform: scale(0.95);
        transition: opacity 0.15s ease, transform 0.15s ease;
        pointer-events: none;
        display: block;
    `;
    
    // Edit Work Item menu item
    const editItem = document.createElement('div');
    editItem.className = 'context-menu-item';
    editItem.style.cssText = `
        padding: 10px 16px;
        cursor: pointer;
        display: flex;
        align-items: center;
        gap: 12px;
        color: ${textColor};
        transition: background-color 0.15s ease;
        user-select: none;
    `;
    editItem.innerHTML = `
        <span class="icon" style="width: 20px; height: 20px; display: flex; align-items: center; justify-content: center; color: ${textColor};">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" width="20" height="20">
                <path d="M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z"/>
            </svg>
        </span>
        <span class="text" style="flex: 1; font-size: 14px; color: ${textColor};">Edit Work Item</span>
    `;
    editItem.setAttribute('data-action', 'edit');
    
    editItem.addEventListener('mouseenter', () => {
        editItem.style.backgroundColor = hoverBg;
    });
    editItem.addEventListener('mouseleave', () => {
        editItem.style.backgroundColor = 'transparent';
    });
    
    contextMenu.appendChild(editItem);
    
    // Schedule menu item
    const scheduleItem = document.createElement('div');
    scheduleItem.className = 'context-menu-item';
    scheduleItem.style.cssText = `
        padding: 10px 16px;
        cursor: pointer;
        display: flex;
        align-items: center;
        gap: 12px;
        color: ${textColor};
        transition: background-color 0.15s ease;
        user-select: none;
    `;
    scheduleItem.innerHTML = `
        <span class="icon" style="width: 20px; height: 20px; display: flex; align-items: center; justify-content: center; color: ${textColor};">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" width="20" height="20">
                <path d="M20 3h-1V1h-2v2H7V1H5v2H4c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 18H4V8h16v13z"/>
            </svg>
        </span>
        <span class="text" style="flex: 1; font-size: 14px; color: ${textColor};">Schedule</span>
    `;
    scheduleItem.setAttribute('data-action', 'schedule');
    
    scheduleItem.addEventListener('mouseenter', () => {
        scheduleItem.style.backgroundColor = hoverBg;
    });
    scheduleItem.addEventListener('mouseleave', () => {
        scheduleItem.style.backgroundColor = 'transparent';
    });
    
    contextMenu.appendChild(scheduleItem);
    document.body.appendChild(contextMenu);
    contextMenu.addEventListener('click', handleContextMenuClick);
}

/**
 * Handle context menu event (right-click)
 */
function handleContextMenu(e) {
    const target = e.target;
    const timelineItem = target.closest('.timeline-item');
    const unscheduledItem = target.closest('.unscheduled-work-item');
    
    if (timelineItem || unscheduledItem) {
        e.preventDefault();
        e.stopPropagation();
        
        const element = timelineItem || unscheduledItem;
        const workItemId = getWorkItemIdFromElement(element);
        
        if (workItemId > 0) {
            showContextMenu(e.clientX, e.clientY, workItemId);
        }
    }
}

/**
 * Show the context menu at the specified position
 */
function showContextMenu(x, y, workItemId) {
    if (!contextMenu) {
        return;
    }
    
    contextMenu.setAttribute('data-workitem-id', workItemId);
    contextMenu.style.left = `${x}px`;
    contextMenu.style.top = `${y}px`;
    contextMenu.style.opacity = '1';
    contextMenu.style.transform = 'scale(1)';
    contextMenu.style.pointerEvents = 'all';
    contextMenu.classList.add('show');
    contextMenuVisible = true;
    
    setTimeout(() => {
        const rect = contextMenu.getBoundingClientRect();
        if (rect.right > window.innerWidth) {
            contextMenu.style.left = `${x - rect.width}px`;
        }
        if (rect.bottom > window.innerHeight) {
            contextMenu.style.top = `${y - rect.height}px`;
        }
    }, 0);
}

/**
 * Hide the context menu
 */
function hideContextMenu() {
    if (!contextMenu || !contextMenuVisible) {
        return;
    }
    
    contextMenu.style.opacity = '0';
    contextMenu.style.transform = 'scale(0.95)';
    contextMenu.style.pointerEvents = 'none';
    contextMenu.classList.remove('show');
    contextMenuVisible = false;
    contextMenu.removeAttribute('data-workitem-id');
}

/**
 * Handle context menu item clicks
 */
function handleContextMenuClick(e) {
    const menuItem = e.target.closest('.context-menu-item');
    
    if (!menuItem) {
        return;
    }
    
    const action = menuItem.getAttribute('data-action');
    const workItemId = parseInt(contextMenu.getAttribute('data-workitem-id'), 10);
    
    if (!dotNetHelper || workItemId <= 0) {
        hideContextMenu();
        return;
    }
    
    try {
        if (action === 'edit') {
            dotNetHelper.invokeMethodAsync('OpenEditWorkItemDialogFromJS', workItemId);
        } else if (action === 'schedule') {
            dotNetHelper.invokeMethodAsync('OpenScheduleDialogFromJS', workItemId);
        }
    } catch (error) {
        console.error(`Error calling ${action} dialog:`, error);
    }
    
    hideContextMenu();
}


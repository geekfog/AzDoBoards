// Ultra-High-Performance Roadmap Date Calculator with Date Tooltips
// Eliminates all lag by using pure client-side DOM manipulation and caching

window.roadmapDateCalculator = {
    
    // Cache DOM elements and timeline data for maximum performance
    _timelineElement: null,
    _indicator: null,
    _initialized: false,
    _timelineData: null,
    _lastX: -1, // Cache last position to avoid unnecessary updates
    
    // Initialize cached references and timeline data
    init: function(timelineElementId, startDate, endDate) {
        if (this._initialized && this._timelineData) return;
        
        this._timelineElement = document.getElementById(timelineElementId || 'timeline-column');
        this._indicator = document.querySelector('.date-drop-indicator');
        
        if (startDate && endDate) {
            this._timelineData = {
                startDate: new Date(startDate),
                endDate: new Date(endDate),
                totalMs: new Date(endDate).getTime() - new Date(startDate).getTime()
            };
        }
        
        if (this._timelineElement && this._indicator) {
            this._initialized = true;
            console.log('Ultra-fast roadmap date calculator initialized');
        }
    },
    
    // Ultra-high-performance visual feedback with date calculation
    updateIndicatorPosition: function(clientX) {
        if (!this._initialized || !this._timelineElement || !this._indicator || !this._timelineData) return;
        
        // Skip if position hasn't changed significantly (performance optimization)
        if (Math.abs(clientX - this._lastX) < 2) return;
        this._lastX = clientX;
        
        const rect = this._timelineElement.getBoundingClientRect();
        const relativeX = Math.max(0, Math.min(rect.width, clientX - rect.left));
        const percentage = relativeX / rect.width;
        
        // Calculate target date inline for maximum performance
        const targetDate = new Date(this._timelineData.startDate.getTime() + (this._timelineData.totalMs * percentage));
        const formattedDate = targetDate.toLocaleDateString('en-US', { 
            month: 'short', 
            day: 'numeric',
            weekday: 'short'
        });
        
        // Use transform for GPU acceleration and add date to indicator
        this._indicator.style.transform = `translateX(${relativeX}px)`;
        this._indicator.setAttribute('data-date', formattedDate);
        this._indicator.classList.add('active');
    },
    
    // Show indicator (for drag enter)
    showIndicator: function() {
        if (this._indicator) {
            this._indicator.classList.add('active');
        }
    },
    
    // Hide indicator
    hideIndicator: function() {
        if (this._indicator) {
            this._indicator.classList.remove('active');
            this._lastX = -1; // Reset cache
        }
    },
    
    // Calculate target date (only called on actual drop)
    calculateTargetDate: function(clientX, timelineStartDate, timelineEndDate, timelineElementId) {
        try {
            // Use cached element if available, otherwise get fresh reference
            const timelineElement = this._timelineElement || document.getElementById(timelineElementId || 'timeline-column');
            if (!timelineElement) {
                console.warn('Timeline element not found');
                return null;
            }

            const rect = timelineElement.getBoundingClientRect();
            const relativeX = Math.max(0, Math.min(rect.width, clientX - rect.left));
            const percentage = relativeX / rect.width;

            const startDate = new Date(timelineStartDate);
            const endDate = new Date(timelineEndDate);
            const totalMs = endDate.getTime() - startDate.getTime();
            
            const targetDate = new Date(startDate.getTime() + (totalMs * percentage));
            
            return {
                date: targetDate.toISOString(),
                percentage: percentage,
                position: relativeX,
                formattedDate: targetDate.toLocaleDateString('en-US', { 
                    month: 'short', 
                    day: 'numeric', 
                    year: 'numeric',
                    weekday: 'short'
                })
            };
        } catch (error) {
            console.error('Error calculating target date:', error);
            return null;
        }
    },

    // Get timeline bounds for validation (cached for performance)
    getTimelineBounds: function(timelineElementId) {
        try {
            const timeline = this._timelineElement || document.getElementById(timelineElementId || 'timeline-column');
            if (!timeline) return null;

            const rect = timeline.getBoundingClientRect();
            return {
                left: rect.left,
                right: rect.right,
                width: rect.width,
                top: rect.top,
                bottom: rect.bottom
            };
        } catch (error) {
            console.error('Error getting timeline bounds:', error);
            return null;
        }
    },

    // Update timeline data when configuration changes
    updateTimelineData: function(startDate, endDate) {
        if (startDate && endDate) {
            this._timelineData = {
                startDate: new Date(startDate),
                endDate: new Date(endDate),
                totalMs: new Date(endDate).getTime() - new Date(startDate).getTime()
            };
        }
    },

    // Cleanup method
    destroy: function() {
        this._timelineElement = null;
        this._indicator = null;
        this._initialized = false;
        this._timelineData = null;
        this._lastX = -1;
    }
};

// Auto-initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        setTimeout(() => window.roadmapDateCalculator.init(), 100);
    });
} else {
    setTimeout(() => window.roadmapDateCalculator.init(), 100);
}

console.log('Ultra-high-performance Roadmap Date Calculator loaded');
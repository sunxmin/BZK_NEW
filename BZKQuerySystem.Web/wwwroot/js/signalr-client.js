// Real-time communication client
// SignalR client manager
class QueryNotificationClient {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.retryCount = 0;
        this.maxRetries = 2; // Further reduce retry attempts
        this.retryInterval = 15000; // 15 seconds
        this.currentQueryId = null;
        this.isInitializing = false;
        this.isEnabled = false; // Disabled by default
        
        // Only initialize SignalR on specific pages
        this.checkIfShouldInitialize();
    }

    // Check if SignalR should be initialized
    checkIfShouldInitialize() {
        // Only enable SignalR on query or monitoring pages
        const currentPath = window.location.pathname.toLowerCase();
        const shouldEnable = currentPath.includes('/querybuilder') || 
                           currentPath.includes('/monitoring') ||
                           currentPath.includes('/query');
        
        if (shouldEnable) {
            this.isEnabled = true;
            // Delayed initialization to ensure page is fully loaded
            setTimeout(() => {
                this.initConnection();
            }, 3000);
        } else {
            console.log('SignalR connection not needed on current page, skipping initialization');
        }
    }

    // Check if SignalR is available
    checkSignalRAvailability() {
        if (typeof signalR === 'undefined') {
            console.info('SignalR library not loaded, real-time features unavailable');
            this.showConnectionStatus('unavailable');
            return false;
        }
        return true;
    }

    // Initialize SignalR connection
    initConnection() {
        if (!this.isEnabled || this.isInitializing) {
            return;
        }
        
        this.isInitializing = true;
        
        // Check SignalR library availability
        if (!this.checkSignalRAvailability()) {
            this.isInitializing = false;
            return;
        }
        
        // Set initial connection status
        this.showConnectionStatus('reconnecting');
        
        try {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/queryNotification', {
                    transport: signalR.HttpTransportType.WebSockets,
                    logMessageContent: false
                })
                .withAutomaticReconnect([0, 5000, 15000]) // Reduce reconnection frequency
                .configureLogging(signalR.LogLevel.Error) // Only record error logs
                .build();

            this.setupEventHandlers();
            this.connect();
        } catch (error) {
            console.warn('SignalR initialization failed, real-time features unavailable:', error.message);
            this.showConnectionStatus('failed');
        } finally {
            this.isInitializing = false;
        }
    }

    // Set event handlers
    setupEventHandlers() {
        // Query progress update
        this.connection.on('QueryProgress', (progress) => {
            this.handleQueryProgress(progress);
        });

        // Query completed
        this.connection.on('QueryCompleted', (result) => {
            this.handleQueryCompleted(result);
        });

        // System notification
        this.connection.on('SystemNotification', (notification) => {
            this.handleSystemNotification(notification);
        });

        // Connection status event
        this.connection.onclose(() => {
            this.isConnected = false;
            console.log('SignalR connection closed');
            this.showConnectionStatus('disconnected');
        });

        this.connection.onreconnecting(() => {
            console.log('SignalR reconnecting...');
            this.showConnectionStatus('reconnecting');
        });

        this.connection.onreconnected(() => {
            this.isConnected = true;
            this.retryCount = 0;
            console.log('SignalR reconnected successfully');
            this.showConnectionStatus('connected');
        });
    }

    // Connect to SignalR Hub
    async connect() {
        try {
            await this.connection.start();
            this.isConnected = true;
            this.retryCount = 0;
            console.log('SignalR connected successfully');
            this.showConnectionStatus('connected');
        } catch (error) {
            console.error('SignalR connection failed:', error);
            this.isConnected = false;
            this.scheduleReconnect();
        }
    }

    // Schedule reconnect
    scheduleReconnect() {
        if (this.retryCount < this.maxRetries) {
            this.retryCount++;
            console.log(`Retry attempt ${this.retryCount} in ${this.retryInterval / 1000} seconds...`);
            
            setTimeout(() => {
                this.connect();
            }, this.retryInterval);
        } else {
            console.error('Maximum retry attempts reached, stopping auto-reconnect');
            this.showConnectionStatus('failed');
        }
    }

    // Show connection status
    showConnectionStatus(status) {
        const indicatorElement = document.getElementById('connection-indicator');
        const textElement = document.getElementById('connection-text');
        
        if (indicatorElement && textElement) {
            const statusConfig = {
                connected: { 
                    color: '#28a745', 
                    text: 'Connected',
                    class: 'text-success'
                },
                disconnected: { 
                    color: '#dc3545', 
                    text: 'Disconnected',
                    class: 'text-danger'
                },
                reconnecting: { 
                    color: '#ffc107', 
                    text: 'Reconnecting...',
                    class: 'text-warning'
                },
                failed: { 
                    color: '#dc3545', 
                    text: 'Connection Failed',
                    class: 'text-danger'
                },
                unavailable: { 
                    color: '#ffc107', 
                    text: 'SignalR Unavailable',
                    class: 'text-warning'
                }
            };
            
            const config = statusConfig[status] || statusConfig.disconnected;
            
            // Set indicator color
            indicatorElement.style.backgroundColor = config.color;
            
            // Set text content and style
            textElement.textContent = config.text;
            textElement.className = `small ${config.class}`;
        }
    }

    // Handle query progress
    handleQueryProgress(progress) {
        console.log('Query progress update:', progress);
        this.updateProgressBar(progress.queryId, progress.progressPercentage, progress.message);
        this.showProgressMessage(progress);
        this.dispatchEvent('queryProgress', progress);
    }

    // Handle query completed
    handleQueryCompleted(result) {
        console.log('Query completed:', result);
        this.hideProgressBar(result.queryId);
        this.showCompletionNotification(result);
        this.dispatchEvent('queryCompleted', result);
    }

    // Handle system notification
    handleSystemNotification(notification) {
        console.log('System notification:', notification);
        this.showToastNotification(notification);
        this.dispatchEvent('systemNotification', notification);
    }

    // Update progress bar
    updateProgressBar(queryId, percentage, message) {
        const progressContainer = document.getElementById('query-progress-container');
        const progressBar = document.getElementById('query-progress-bar');
        const progressText = document.getElementById('query-progress-text');
        const progressMessage = document.getElementById('query-progress-message');

        if (progressContainer && progressBar && progressText) {
            progressContainer.style.display = 'block';
            progressBar.style.width = `${percentage}%`;
            progressBar.setAttribute('aria-valuenow', percentage);
            progressText.textContent = `${percentage}%`;
            
            if (progressMessage && message) {
                progressMessage.textContent = message;
            }
        }
    }

    // Hide progress bar
    hideProgressBar(queryId) {
        const progressContainer = document.getElementById('query-progress-container');
        if (progressContainer) {
            setTimeout(() => {
                progressContainer.style.display = 'none';
            }, 1000);
        }
    }

    // Show progress message
    showProgressMessage(progress) {
        const statusDiv = document.getElementById('query-status');
        if (statusDiv) {
            statusDiv.innerHTML = `
                <div class="alert alert-info">
                    <i class="fas fa-spinner fa-spin"></i>
                    ${progress.message || 'Query execution in progress...'}
                    <span class="badge bg-primary ms-2">${progress.progressPercentage}%</span>
                </div>
            `;
        }
    }

    // Show completion notification
    showCompletionNotification(result) {
        const alertClass = result.isSuccess ? 'alert-success' : 'alert-danger';
        const icon = result.isSuccess ? 'fa-check-circle' : 'fa-exclamation-circle';
        const title = result.isSuccess ? 'Query completed' : 'Query failed';
        
        let message = '';
        if (result.isSuccess) {
            message = `Query completed successfully, processed ${result.recordCount} records, took ${this.formatDuration(result.executionTime)}`;
        } else {
            message = result.errorMessage || 'Query execution failed';
        }

        this.showToastNotification({
            title: title,
            message: message,
            type: result.isSuccess ? 'Success' : 'Error',
            autoHideDelay: result.isSuccess ? 5000 : 10000
        });
    }

    // Show Toast notification
    showToastNotification(notification) {
        const toastHtml = this.createToastHtml(notification);
        const toastContainer = this.getOrCreateToastContainer();
        
        toastContainer.insertAdjacentHTML('beforeend', toastHtml);
        
        const toastElement = toastContainer.lastElementChild;
        const toast = new bootstrap.Toast(toastElement, {
            autohide: notification.autoHideDelay ? true : false,
            delay: notification.autoHideDelay || 5000
        });
        
        toast.show();
        
        toastElement.addEventListener('hidden.bs.toast', () => {
            toastElement.remove();
        });
    }

    // Create Toast HTML
    createToastHtml(notification) {
        const typeClass = this.getToastTypeClass(notification.type);
        const icon = this.getToastIcon(notification.type);
        
        return `
            <div class="toast ${typeClass}" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="toast-header">
                    <i class="fas ${icon} me-2"></i>
                    <strong class="me-auto">${notification.title || 'System notification'}</strong>
                    <small class="text-muted">${this.formatTime(notification.timestamp)}</small>
                    <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
                <div class="toast-body">
                    ${notification.message}
                </div>
            </div>
        `;
    }

    // Get or create Toast container
    getOrCreateToastContainer() {
        let container = document.getElementById('toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toast-container';
            container.className = 'toast-container position-fixed top-0 end-0 p-3';
            container.style.zIndex = '1050';
            document.body.appendChild(container);
        }
        return container;
    }

    // Get Toast type class
    getToastTypeClass(type) {
        switch (type) {
            case 'Success': return 'text-bg-success';
            case 'Warning': return 'text-bg-warning';
            case 'Error': return 'text-bg-danger';
            default: return 'text-bg-info';
        }
    }

    // Get Toast icon
    getToastIcon(type) {
        switch (type) {
            case 'Success': return 'fa-check-circle';
            case 'Warning': return 'fa-exclamation-triangle';
            case 'Error': return 'fa-exclamation-circle';
            default: return 'fa-info-circle';
        }
    }

    // Format time
    formatTime(timestamp) {
        return new Date(timestamp).toLocaleTimeString();
    }

    // Format duration
    formatDuration(timeSpan) {
        const parts = timeSpan.split(':');
        if (parts.length >= 3) {
            const hours = parseInt(parts[0]);
            const minutes = parseInt(parts[1]);
            const seconds = parseFloat(parts[2]);
            
            if (hours > 0) {
                return `${hours} hours ${minutes} minutes ${seconds.toFixed(1)} seconds`;
            } else if (minutes > 0) {
                return `${minutes} minutes ${seconds.toFixed(1)} seconds`;
            } else {
                return `${seconds.toFixed(1)} seconds`;
            }
        }
        return timeSpan;
    }

    // Dispatch event
    dispatchEvent(eventName, data) {
        const event = new CustomEvent(`signalr-${eventName}`, {
            detail: data,
            bubbles: true
        });
        document.dispatchEvent(event);
    }
}

// Global instance
let queryNotificationClient = null;

// Initialize SignalR client
document.addEventListener('DOMContentLoaded', function() {
    if (typeof signalR !== 'undefined') {
        queryNotificationClient = new QueryNotificationClient();
        window.queryNotificationClient = queryNotificationClient;
        console.log('SignalR client initialized');
    } else {
        console.error('SignalR library not loaded, real-time communication features unavailable');
    }
});

// Global event handler
document.addEventListener('signalr-queryProgress', function(event) {
    console.log('Received query progress event:', event.detail);
});

document.addEventListener('signalr-queryCompleted', function(event) {
    console.log('Received query completed event:', event.detail);
});

document.addEventListener('signalr-systemNotification', function(event) {
    console.log('Received system notification event:', event.detail);
}); 
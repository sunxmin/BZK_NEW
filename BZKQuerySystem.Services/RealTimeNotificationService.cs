using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace BZKQuerySystem.Services
{
    public interface IRealTimeNotificationService
    {
        Task SendQueryProgressAsync(string userId, QueryProgressUpdate progress);
        Task SendQueryCompletedAsync(string userId, QueryCompletionResult result);
        Task SendSystemNotificationAsync(string userId, SystemNotification notification);
        Task SendGlobalNotificationAsync(SystemNotification notification);
        Task JoinQueryGroupAsync(string connectionId, string queryId);
        Task LeaveQueryGroupAsync(string connectionId, string queryId);
    }

    public class QueryProgressUpdate
    {
        public string QueryId { get; set; } = "";
        public string Status { get; set; } = "";
        public int ProgressPercentage { get; set; }
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class QueryCompletionResult
    {
        public string QueryId { get; set; } = "";
        public bool IsSuccess { get; set; }
        public int RecordCount { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.Now;
    }

    public class SystemNotification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public NotificationType Type { get; set; } = NotificationType.Info;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsGlobal { get; set; } = false;
        public TimeSpan? AutoHideDelay { get; set; }
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public class RealTimeNotificationService : IRealTimeNotificationService
    {
        private readonly IHubContext<QueryNotificationHub> _hubContext;
        private readonly ILogger<RealTimeNotificationService> _logger;

        public RealTimeNotificationService(
            IHubContext<QueryNotificationHub> hubContext,
            ILogger<RealTimeNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task SendQueryProgressAsync(string userId, QueryProgressUpdate progress)
        {
            try
            {
                await _hubContext.Clients.User(userId).SendAsync("QueryProgress", progress);
                _logger.LogDebug("���Ͳ�ѯ����֪ͨ: �û� {UserId}, ��ѯ {QueryId}, ���� {Progress}%", 
                    userId, progress.QueryId, progress.ProgressPercentage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���Ͳ�ѯ����֪ͨʧ��: �û� {UserId}, ��ѯ {QueryId}", 
                    userId, progress.QueryId);
            }
        }

        public async Task SendQueryCompletedAsync(string userId, QueryCompletionResult result)
        {
            try
            {
                await _hubContext.Clients.User(userId).SendAsync("QueryCompleted", result);
                _logger.LogInformation("���Ͳ�ѯ���֪ͨ: �û� {UserId}, ��ѯ {QueryId}, �ɹ� {IsSuccess}", 
                    userId, result.QueryId, result.IsSuccess);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���Ͳ�ѯ���֪ͨʧ��: �û� {UserId}, ��ѯ {QueryId}", 
                    userId, result.QueryId);
            }
        }

        public async Task SendSystemNotificationAsync(string userId, SystemNotification notification)
        {
            try
            {
                await _hubContext.Clients.User(userId).SendAsync("SystemNotification", notification);
                _logger.LogDebug("����ϵͳ֪ͨ: �û� {UserId}, ���� {Type}, ���� {Title}", 
                    userId, notification.Type, notification.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "����ϵͳ֪ͨʧ��: �û� {UserId}, ֪ͨ {NotificationId}", 
                    userId, notification.Id);
            }
        }

        public async Task SendGlobalNotificationAsync(SystemNotification notification)
        {
            try
            {
                notification.IsGlobal = true;
                await _hubContext.Clients.All.SendAsync("SystemNotification", notification);
                _logger.LogInformation("����ȫ��֪ͨ: ���� {Type}, ���� {Title}", 
                    notification.Type, notification.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "����ȫ��֪ͨʧ��: ֪ͨ {NotificationId}", notification.Id);
            }
        }

        public async Task JoinQueryGroupAsync(string connectionId, string queryId)
        {
            try
            {
                await _hubContext.Groups.AddToGroupAsync(connectionId, $"query_{queryId}");
                _logger.LogDebug("���� {ConnectionId} �����ѯ�� {QueryId}", connectionId, queryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�����ѯ��ʧ��: ���� {ConnectionId}, ��ѯ {QueryId}", 
                    connectionId, queryId);
            }
        }

        public async Task LeaveQueryGroupAsync(string connectionId, string queryId)
        {
            try
            {
                await _hubContext.Groups.RemoveFromGroupAsync(connectionId, $"query_{queryId}");
                _logger.LogDebug("���� {ConnectionId} �뿪��ѯ�� {QueryId}", connectionId, queryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�뿪��ѯ��ʧ��: ���� {ConnectionId}, ��ѯ {QueryId}", 
                    connectionId, queryId);
            }
        }
    }

    // SignalR Hub��
    public class QueryNotificationHub : Hub
    {
        private readonly ILogger<QueryNotificationHub> _logger;

        public QueryNotificationHub(ILogger<QueryNotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.Identity?.Name;
            _logger.LogInformation("�û�����: {UserId}, ����ID: {ConnectionId}", userId, Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.Identity?.Name;
            _logger.LogInformation("�û��Ͽ�����: {UserId}, ����ID: {ConnectionId}", userId, Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinQueryGroup(string queryId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"query_{queryId}");
            _logger.LogDebug("���� {ConnectionId} �����ѯ�� {QueryId}", Context.ConnectionId, queryId);
        }

        public async Task LeaveQueryGroup(string queryId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"query_{queryId}");
            _logger.LogDebug("���� {ConnectionId} �뿪��ѯ�� {QueryId}", Context.ConnectionId, queryId);
        }
    }
} 
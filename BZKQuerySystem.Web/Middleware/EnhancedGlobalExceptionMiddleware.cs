using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using BZKQuerySystem.Services;

namespace BZKQuerySystem.Web.Middleware
{
    /// <summary>
    /// 增强的全局异常处理中间件
    /// 提供统一的异常处理、日志记录和错误响应格式
    /// </summary>
    public class EnhancedGlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<EnhancedGlobalExceptionMiddleware> _logger;
        private readonly IAuditService? _auditService;

        public EnhancedGlobalExceptionMiddleware(
            RequestDelegate next, 
            ILogger<EnhancedGlobalExceptionMiddleware> logger,
            IAuditService? auditService = null)
        {
            _next = next;
            _logger = logger;
            _auditService = auditService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await HandleExceptionAsync(context, ex, traceId, stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception, string traceId, long elapsedMs)
        {
            // 记录详细的异常信息
            _logger.LogError(exception, 
                "未处理的异常发生 | TraceId: {TraceId} | Path: {Path} | Method: {Method} | User: {User} | Duration: {Duration}ms",
                traceId,
                context.Request.Path,
                context.Request.Method,
                context.User?.Identity?.Name ?? "Anonymous",
                elapsedMs);

            // 记录审计日志
            if (_auditService != null)
            {
                try
                {
                    await _auditService.LogSecurityEventAsync(
                        context.User?.Identity?.Name ?? "Anonymous",
                        "UnhandledException",
                        $"Path: {context.Request.Path}, Exception: {exception.GetType().Name}",
                        context.Connection.RemoteIpAddress?.ToString());
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "记录审计日志失败");
                }
            }

            // 确定HTTP状态码和错误类型
            var (statusCode, errorType, userMessage) = DetermineErrorResponse(exception);

            // 设置响应
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            // 构建错误响应
            var errorResponse = new EnhancedApiErrorResponse
            {
                StatusCode = statusCode,
                ErrorType = errorType,
                Message = userMessage,
                TraceId = traceId,
                Timestamp = DateTime.UtcNow,
                Path = context.Request.Path,
                Method = context.Request.Method
            };

            // 在开发环境中包含详细错误信息
            if (IsDevelopmentEnvironment())
            {
                errorResponse.Details = exception.ToString();
                errorResponse.StackTrace = exception.StackTrace;
            }

            var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        /// <summary>
        /// 根据异常类型确定错误响应
        /// </summary>
        private static (int statusCode, string errorType, string userMessage) DetermineErrorResponse(Exception exception)
        {
            return exception switch
            {
                ArgumentNullException => (400, "ValidationError", "必需参数缺失"),
                ArgumentException => (400, "ValidationError", "请求参数无效"),
                UnauthorizedAccessException => (401, "AuthenticationError", "身份验证失败"),
                EnhancedSecurityException => (403, "SecurityError", "安全验证失败"),
                KeyNotFoundException => (404, "NotFoundError", "请求的资源不存在"),
                NotSupportedException => (405, "MethodNotAllowed", "不支持的操作"),
                TimeoutException => (408, "TimeoutError", "请求超时"),
                InvalidOperationException => (409, "ConflictError", "操作冲突"),
                NotImplementedException => (501, "NotImplemented", "功能尚未实现"),
                _ => (500, "InternalServerError", "服务器内部错误")
            };
        }

        /// <summary>
        /// 检查是否为开发环境
        /// </summary>
        private static bool IsDevelopmentEnvironment()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            return string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// 增强的API错误响应模型
    /// </summary>
    public class EnhancedApiErrorResponse
    {
        public int StatusCode { get; set; }
        public string ErrorType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? StackTrace { get; set; }
    }

    /// <summary>
    /// 增强的安全异常类
    /// </summary>
    public class EnhancedSecurityException : Exception
    {
        public EnhancedSecurityException(string message) : base(message) { }
        public EnhancedSecurityException(string message, Exception innerException) : base(message, innerException) { }
    }
} 
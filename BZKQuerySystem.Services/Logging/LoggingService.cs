using Microsoft.Extensions.Logging;
using System;

namespace BZKQuerySystem.Services.Logging
{
    /// <summary>
    /// 统一的日志服务接口
    /// </summary>
    public interface ILoggingService
    {
        void LogInformation(string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogError(string message, params object[] args);
        void LogError(Exception exception, string message, params object[] args);
        void LogDebug(string message, params object[] args);
        void LogTrace(string message, params object[] args);
    }

    /// <summary>
    /// 统一的日志服务实现
    /// 用于替换项目中的Console.WriteLine调用
    /// </summary>
    public class LoggingService : ILoggingService
    {
        private readonly ILogger<LoggingService> _logger;

        public LoggingService(ILogger<LoggingService> logger)
        {
            _logger = logger;
        }

        public void LogInformation(string message, params object[] args)
        {
            _logger.LogInformation(message, args);
        }

        public void LogWarning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
        }

        public void LogError(string message, params object[] args)
        {
            _logger.LogError(message, args);
        }

        public void LogError(Exception exception, string message, params object[] args)
        {
            _logger.LogError(exception, message, args);
        }

        public void LogDebug(string message, params object[] args)
        {
            _logger.LogDebug(message, args);
        }

        public void LogTrace(string message, params object[] args)
        {
            _logger.LogTrace(message, args);
        }
    }

    /// <summary>
    /// 静态日志帮助类
    /// 提供简化的日志记录方法
    /// </summary>
    public static class LogHelper
    {
        private static ILoggingService? _loggingService;

        /// <summary>
        /// 初始化日志服务
        /// </summary>
        public static void Initialize(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        /// <summary>
        /// 记录信息日志
        /// </summary>
        public static void Info(string message, params object[] args)
        {
            _loggingService?.LogInformation(message, args);
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        public static void Warning(string message, params object[] args)
        {
            _loggingService?.LogWarning(message, args);
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        public static void Error(string message, params object[] args)
        {
            _loggingService?.LogError(message, args);
        }

        /// <summary>
        /// 记录异常日志
        /// </summary>
        public static void Error(Exception exception, string message, params object[] args)
        {
            _loggingService?.LogError(exception, message, args);
        }

        /// <summary>
        /// 记录调试日志
        /// </summary>
        public static void Debug(string message, params object[] args)
        {
            _loggingService?.LogDebug(message, args);
        }

        /// <summary>
        /// 记录跟踪日志
        /// </summary>
        public static void Trace(string message, params object[] args)
        {
            _loggingService?.LogTrace(message, args);
        }
    }
} 
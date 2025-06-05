using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BZKQuerySystem.Services.Quality;
using BZKQuerySystem.Services.Extensions;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace BZKQuerySystem.Web.Controllers
{
    /// <summary>
    /// 代码质量管理控制器
    /// 第一阶段优化：代码质量与架构稳固
    /// </summary>
    [Authorize]
    public class CodeQualityController : Controller
    {
        private readonly ILogger<CodeQualityController> _logger;
        private readonly IPerformanceTracker _performanceTracker;
        private readonly ICodeAnalyzer _codeAnalyzer;
        private readonly IHealthChecker _healthChecker;
        private readonly ILoggerFactory _loggerFactory;

        public CodeQualityController(
            ILogger<CodeQualityController> logger,
            IPerformanceTracker performanceTracker,
            ICodeAnalyzer codeAnalyzer,
            IHealthChecker healthChecker,
            ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _performanceTracker = performanceTracker;
            _codeAnalyzer = codeAnalyzer;
            _healthChecker = healthChecker;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// 代码质量仪表板
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("访问代码质量仪表板");

                var model = new CodeQualityDashboardViewModel
                {
                    LastAnalysisDate = DateTime.UtcNow.AddHours(-2), // 模拟上次分析时间
                    OverallScore = 85,
                    TotalIssues = 23,
                    CriticalIssues = 2,
                    WarningIssues = 15,
                    InfoIssues = 6
                };

                // 获取系统健康状态
                var dbHealth = await _healthChecker.CheckDatabaseHealthAsync();
                var cacheHealth = await _healthChecker.CheckCacheHealthAsync();
                var systemHealth = await _healthChecker.CheckSystemHealthAsync();

                model.SystemHealth = new SystemHealthViewModel
                {
                    DatabaseStatus = dbHealth.Status,
                    CacheStatus = cacheHealth.Status,
                    SystemStatus = systemHealth.Status,
                    OverallStatus = DetermineOverallHealth(dbHealth, cacheHealth, systemHealth)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取代码质量仪表板数据时发生错误");
                return View("Error");
            }
        }

        /// <summary>
        /// 执行代码质量分析
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RunAnalysis()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation("开始执行代码质量分析");

                // 获取项目根路径
                var projectPath = GetProjectRootPath();
                var checkerLogger = _loggerFactory.CreateLogger<CodeQualityChecker>();
                var checker = new CodeQualityChecker(checkerLogger, projectPath);
                
                var report = await checker.RunFullAnalysisAsync();
                
                stopwatch.Stop();
                _performanceTracker.TrackOperation("CodeQualityAnalysis", stopwatch.Elapsed, true);

                return Json(new
                {
                    success = true,
                    message = "代码质量分析完成",
                    data = new
                    {
                        totalFiles = report.TotalFiles,
                        totalLines = report.TotalLines,
                        issueCount = report.IssueCount,
                        qualityScore = report.QualityScore,
                        analysisDate = report.AnalysisDate,
                        issuesBySeverity = report.IssuesBySeverity,
                        issuesByCategory = report.IssuesByCategory,
                        duration = stopwatch.ElapsedMilliseconds
                    }
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _performanceTracker.TrackException("CodeQualityAnalysis", ex);
                _logger.LogError(ex, "代码质量分析过程中发生错误");

                return Json(new
                {
                    success = false,
                    message = "代码质量分析失败",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 获取详细的问题列表
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetIssues(string severity = "", string category = "")
        {
            try
            {
                _logger.LogInformation("获取代码质量问题列表，筛选条件：严重程度={Severity}, 类别={Category}", severity, category);

                // 获取项目根路径
                var projectPath = GetProjectRootPath();
                var checkerLogger = _loggerFactory.CreateLogger<CodeQualityChecker>();
                var checker = new CodeQualityChecker(checkerLogger, projectPath);
                
                var report = await checker.RunFullAnalysisAsync();
                var issues = report.Issues;

                // 应用筛选条件
                if (!string.IsNullOrEmpty(severity))
                {
                    issues = issues.Where(i => i.Severity.Equals(severity, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrEmpty(category))
                {
                    issues = issues.Where(i => i.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                return Json(new
                {
                    success = true,
                    data = issues.Select(i => new
                    {
                        file = i.File,
                        line = i.Line,
                        severity = i.Severity,
                        category = i.Category,
                        message = i.Message,
                        suggestion = i.Suggestion
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取代码质量问题列表时发生错误");
                return Json(new
                {
                    success = false,
                    message = "获取问题列表失败",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 获取优化建议
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOptimizationSuggestions()
        {
            try
            {
                _logger.LogInformation("获取代码优化建议");

                var suggestions = new List<OptimizationSuggestion>
                {
                    new OptimizationSuggestion
                    {
                        Priority = "High",
                        Category = "Logging",
                        Title = "统一日志系统",
                        Description = "将所有Console.WriteLine调用替换为统一的日志系统",
                        Impact = "提高日志管理效率，便于问题排查",
                        EstimatedEffort = "2-3天",
                        Status = "In Progress"
                    },
                    new OptimizationSuggestion
                    {
                        Priority = "High",
                        Category = "Exception Handling",
                        Title = "增强异常处理",
                        Description = "完善异常处理机制，确保所有异常都被正确记录",
                        Impact = "提高系统稳定性和可维护性",
                        EstimatedEffort = "3-4天",
                        Status = "Planned"
                    },
                    new OptimizationSuggestion
                    {
                        Priority = "Medium",
                        Category = "Documentation",
                        Title = "完善代码注释",
                        Description = "为公共方法添加XML文档注释",
                        Impact = "提高代码可读性和维护性",
                        EstimatedEffort = "1-2天",
                        Status = "Planned"
                    },
                    new OptimizationSuggestion
                    {
                        Priority = "Medium",
                        Category = "Testing",
                        Title = "增加单元测试",
                        Description = "为核心业务逻辑添加单元测试",
                        Impact = "提高代码质量和回归测试能力",
                        EstimatedEffort = "5-7天",
                        Status = "Planned"
                    },
                    new OptimizationSuggestion
                    {
                        Priority = "Low",
                        Category = "Performance",
                        Title = "性能监控增强",
                        Description = "添加更详细的性能监控指标",
                        Impact = "便于性能问题识别和优化",
                        EstimatedEffort = "2-3天",
                        Status = "Planned"
                    }
                };

                return Json(new
                {
                    success = true,
                    data = suggestions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取优化建议时发生错误");
                return Json(new
                {
                    success = false,
                    message = "获取优化建议失败",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 获取项目根路径
        /// </summary>
        private string GetProjectRootPath()
        {
            // 获取当前应用程序的根目录
            var currentDirectory = Directory.GetCurrentDirectory();
            
            // 向上查找包含.sln文件的目录
            var directory = new DirectoryInfo(currentDirectory);
            while (directory != null && !directory.GetFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? currentDirectory;
        }

        /// <summary>
        /// 确定整体健康状态
        /// </summary>
        private string DetermineOverallHealth(params HealthStatus[] healthStatuses)
        {
            if (healthStatuses.All(h => h.IsHealthy))
                return "Healthy";
            
            if (healthStatuses.Any(h => !h.IsHealthy))
                return "Critical";
            
            return "Warning";
        }
    }

    /// <summary>
    /// 代码质量仪表板视图模型
    /// </summary>
    public class CodeQualityDashboardViewModel
    {
        public DateTime LastAnalysisDate { get; set; }
        public int OverallScore { get; set; }
        public int TotalIssues { get; set; }
        public int CriticalIssues { get; set; }
        public int WarningIssues { get; set; }
        public int InfoIssues { get; set; }
        public SystemHealthViewModel SystemHealth { get; set; } = new();
    }

    /// <summary>
    /// 系统健康状态视图模型
    /// </summary>
    public class SystemHealthViewModel
    {
        public string DatabaseStatus { get; set; } = string.Empty;
        public string CacheStatus { get; set; } = string.Empty;
        public string SystemStatus { get; set; } = string.Empty;
        public string OverallStatus { get; set; } = string.Empty;
    }

    /// <summary>
    /// 优化建议模型
    /// </summary>
    public class OptimizationSuggestion
    {
        public string Priority { get; set; } = string.Empty; // High, Medium, Low
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
        public string EstimatedEffort { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Planned, In Progress, Completed
    }
} 
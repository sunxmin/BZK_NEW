using Microsoft.AspNetCore.Mvc;
using BZKQuerySystem.Services;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;

namespace BZKQuerySystem.Web.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PerformanceController : ControllerBase
    {
        private readonly IQueryPerformanceService _performanceService;
        private readonly ILogger<PerformanceController> _logger;

        public PerformanceController(
            IQueryPerformanceService performanceService,
            ILogger<PerformanceController> logger)
        {
            _performanceService = performanceService;
            _logger = logger;
        }

        /// <summary>
        /// 获取查询性能统计信息
        /// </summary>
        [HttpGet("stats")]
        public ActionResult<QueryPerformanceStats> GetPerformanceStats()
        {
            try
            {
                var stats = _performanceService.GetPerformanceStats();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取性能统计失败");
                return StatusCode(500, "获取性能统计失败");
            }
        }

        /// <summary>
        /// 获取慢查询列表
        /// </summary>
        [HttpGet("slow-queries")]
        public ActionResult<List<QueryPerformanceResult>> GetSlowQueries([FromQuery] int count = 10)
        {
            try
            {
                var slowQueries = _performanceService.GetSlowQueries(count);
                return Ok(slowQueries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取慢查询列表失败");
                return StatusCode(500, "获取慢查询列表失败");
            }
        }

        /// <summary>
        /// 清空性能统计数据
        /// </summary>
        [HttpPost("clear-stats")]
        public ActionResult ClearStats()
        {
            try
            {
                _performanceService.ClearStats();
                _logger.LogInformation("性能统计数据已清空");
                return Ok(new { message = "性能统计数据已清空" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清空性能统计失败");
                return StatusCode(500, "清空性能统计失败");
            }
        }

        /// <summary>
        /// 获取系统信息
        /// </summary>
        [HttpGet("system-info")]
        public ActionResult<object> GetSystemInfo()
        {
            try
            {
                var systemInfo = new
                {
                    Environment = Environment.OSVersion.ToString(),
                    MachineName = Environment.MachineName,
                    ProcessorCount = Environment.ProcessorCount,
                    WorkingSet = Environment.WorkingSet,
                    GCTotalMemory = GC.GetTotalMemory(false),
                    ServerTime = DateTime.Now,
                    ApplicationUptime = DateTime.Now - Process.GetCurrentProcess().StartTime
                };

                return Ok(systemInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取系统信息失败");
                return StatusCode(500, "获取系统信息失败");
            }
        }
    }
} 
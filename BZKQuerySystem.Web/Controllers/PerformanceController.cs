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
        /// ��ȡ��ѯ����ͳ����Ϣ
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
                _logger.LogError(ex, "��ȡ����ͳ��ʧ��");
                return StatusCode(500, "��ȡ����ͳ��ʧ��");
            }
        }

        /// <summary>
        /// ��ȡ����ѯ�б�
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
                _logger.LogError(ex, "��ȡ����ѯ�б�ʧ��");
                return StatusCode(500, "��ȡ����ѯ�б�ʧ��");
            }
        }

        /// <summary>
        /// �������ͳ������
        /// </summary>
        [HttpPost("clear-stats")]
        public ActionResult ClearStats()
        {
            try
            {
                _performanceService.ClearStats();
                _logger.LogInformation("����ͳ�����������");
                return Ok(new { message = "����ͳ�����������" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�������ͳ��ʧ��");
                return StatusCode(500, "�������ͳ��ʧ��");
            }
        }

        /// <summary>
        /// ��ȡϵͳ��Ϣ
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
                _logger.LogError(ex, "��ȡϵͳ��Ϣʧ��");
                return StatusCode(500, "��ȡϵͳ��Ϣʧ��");
            }
        }
    }
} 
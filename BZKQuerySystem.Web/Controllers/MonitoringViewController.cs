using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BZKQuerySystem.Web.Controllers
{
    /// <summary>
    /// 监控系统视图控制器
    /// </summary>
    [Authorize(Policy = "SystemAdmin")]
    public class MonitoringViewController : Controller
    {
        /// <summary>
        /// 监控仪表板页面
        /// </summary>
        [HttpGet("/Monitoring/Dashboard")]
        public IActionResult Dashboard()
        {
            ViewData["Title"] = "系统监控仪表板";
            return View("~/Views/Monitoring/Dashboard.cshtml");
        }
    }
} 
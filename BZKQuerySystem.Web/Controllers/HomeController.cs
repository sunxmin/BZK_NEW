using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BZKQuerySystem.Web.Models;

namespace BZKQuerySystem.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        try
        {
            _logger.LogInformation("访问主页开始");
            
            // 检查用户未认证，重定向到登录页面
            if (User?.Identity?.IsAuthenticated != true)
            {
                _logger.LogInformation("用户未认证，重定向到登录页面");
                return RedirectToAction("Login", "Account");
            }
            
            ViewBag.Title = "专病多维度查询系统 - 主页";
            
            // 安全地获取用户信息
            var userName = User.Identity.Name ?? "未认证用户";
            var displayName = User.FindFirst("DisplayName")?.Value ?? userName;
            _logger.LogInformation("用户身份认证: {UserName}", userName);
            
            ViewBag.UserName = userName;
            ViewBag.DisplayName = displayName;
            ViewBag.IsAuthenticated = true;
            
            _logger.LogInformation("主页数据准备完成，准备返回视图");
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "访问主页时发生错误: {ErrorMessage}", ex.Message);
            
            // 返回错误视图模型
            var errorModel = new ErrorViewModel 
            { 
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                ErrorMessage = ex.Message
            };
            
            return View("Error", errorModel);
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

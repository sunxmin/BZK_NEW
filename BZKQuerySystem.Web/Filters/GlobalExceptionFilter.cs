using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace BZKQuerySystem.Web.Filters
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, "未处理的异常: {Message}", context.Exception.Message);

            if (context.HttpContext.Request.Headers.Accept.ToString().Contains("application/json"))
            {
                // API 请求返回 JSON 错误
                context.Result = new JsonResult(new 
                { 
                    error = "服务器内部错误",
                    message = context.Exception.Message 
                })
                {
                    StatusCode = 500
                };
            }
            else
            {
                // 网页请求重定向到错误页面
                context.Result = new RedirectToActionResult("Error", "Home", null);
            }

            context.ExceptionHandled = true;
        }
    }
} 
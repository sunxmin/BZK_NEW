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
            _logger.LogError(context.Exception, "δ������쳣: {Message}", context.Exception.Message);

            if (context.HttpContext.Request.Headers.Accept.ToString().Contains("application/json"))
            {
                // API ���󷵻� JSON ����
                context.Result = new JsonResult(new 
                { 
                    error = "�������ڲ�����",
                    message = context.Exception.Message 
                })
                {
                    StatusCode = 500
                };
            }
            else
            {
                // ��ҳ�����ض��򵽴���ҳ��
                context.Result = new RedirectToActionResult("Error", "Home", null);
            }

            context.ExceptionHandled = true;
        }
    }
} 
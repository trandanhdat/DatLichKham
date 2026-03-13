using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DatLichKham.Services
{
    public class SessionCheckFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Danh sách các Controller/Action KHÔNG cần đăng nhập (ví dụ: Login, Register)
            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();

            if (controllerName == "Account" && (actionName == "Login" || actionName == "Register"))
            {
                return; // Cho phép đi qua
            }

            // Kiểm tra Session
            var userId = context.HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                // Nếu chưa đăng nhập, đá về trang Login
                context.Result = new RedirectToActionResult("Login", "Account", null);
            }
        }
        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}

using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Authorization;
using OneForAll.Core;
using OneForAll.Core.Extension;
using ScheduleJob.HttpService.Interfaces;
using ScheduleJob.Host.Models;
using OneForAll.Core.Security;

namespace ScheduleJob.Host.Filters
{
    /// <summary>
    /// 过滤器：全局授权
    /// </summary>
    public class AuthorizationFilter : IAuthorizationFilter
    {
        private readonly AuthConfig _config;
        private readonly ISysPermissionCheckHttpService _httpPermService;
        public AuthorizationFilter(AuthConfig config, ISysPermissionCheckHttpService httpPermService)
        {
            _config = config;
            _httpPermService = httpPermService;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context.Filters.Any(item => item is IAllowAnonymousFilter) ||
                !(context.ActionDescriptor is ControllerActionDescriptor))
            {
                return;
            }

            var attrs = new List<object>();
            attrs.AddRange((context.ActionDescriptor as ControllerActionDescriptor).MethodInfo.GetCustomAttributes(true));
            attrs.AddRange((context.ActionDescriptor as ControllerActionDescriptor).MethodInfo.DeclaringType.GetCustomAttributes(true));
            var checkPermAttrs = attrs.OfType<CheckPermissionAttribute>().ToList();
            var claims = context.HttpContext.User.Claims;
            if (checkPermAttrs.Count > 0)
            {
                var controller = context.ActionDescriptor.RouteValues["controller"];
                var action = context.ActionDescriptor.RouteValues["action"];
                if (!checkPermAttrs.First().Controller.IsNullOrEmpty()) controller = checkPermAttrs.First().Controller;
                if (!checkPermAttrs.First().Action.IsNullOrEmpty()) action = checkPermAttrs.First().Action;
                var msg = _httpPermService.ValidateAuthorization(controller, action).Result;
                if (msg.ErrType.Equals(BaseErrType.Success))
                {
                    return;
                }
                else
                {
                    context.Result = new JsonResult(msg);
                }
            }
        }
    }

    /// <summary>
    /// 权限检测
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class CheckPermissionAttribute : AuthorizeAttribute
    {
        public string Controller { get; set; }
        public string Action { get; set; }

    }
}

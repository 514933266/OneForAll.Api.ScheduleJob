using Microsoft.AspNetCore.Http;
using OneForAll.Core.Extension;
using OneForAll.Core.OAuth;
using ScheduleJob.Domain.Interfaces;
using System.Linq;

namespace ScheduleJob.Host.Providers
{
    /// <summary>
    /// 当前请求上下文端口实现：从 JWT Claims / HttpContext 解析登录用户与来源IP
    /// </summary>
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 当前登录用户信息
        /// </summary>
        public LoginUser LoginUser
        {
            get
            {
                var claims = _httpContextAccessor.HttpContext?.User?.Claims;
                if (claims != null && claims.Any())
                {
                    return new LoginUser
                    {
                        DisplayName = claims.FirstOrDefault(e => e.Type == UserClaimType.DisplayName)?.Value ?? "",
                        UserName = claims.FirstOrDefault(e => e.Type == UserClaimType.UserName)?.Value ?? "",
                        WxAppId = claims.FirstOrDefault(e => e.Type == UserClaimType.WxAppId)?.Value ?? "",
                        WxOpenId = claims.FirstOrDefault(e => e.Type == UserClaimType.WxOpenId)?.Value ?? "",
                        WxUnionId = claims.FirstOrDefault(e => e.Type == UserClaimType.WxUnionId)?.Value ?? "",
                        Id = claims.FirstOrDefault(e => e.Type == UserClaimType.UserId)?.Value ?? "",
                        TenantId = claims.FirstOrDefault(e => e.Type == UserClaimType.TenantId)?.Value ?? "",
                        IsDefault = claims.FirstOrDefault(e => e.Type == UserClaimType.IsDefault)?.Value.TryBoolean() ?? false
                    };
                }
                return new LoginUser();
            }
        }

        /// <summary>
        /// 当前请求来源IP（无请求上下文时为空字符串）
        /// </summary>
        public string IpAddress
        {
            get
            {
                var context = _httpContextAccessor.HttpContext;
                if (context == null || context.Connection.RemoteIpAddress == null)
                    return "";
                return context.Connection.RemoteIpAddress.ToString();
            }
        }
    }
}

using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using OneForAll.Core.Extension;
using ScheduleJob.Public.Models;
using OneForAll.Core.OAuth;

namespace ScheduleJob.HttpService
{
    /// <summary>
    /// Http基类
    /// </summary>
    public class BaseHttpService
    {
        private readonly string AUTH_KEY = "Authorization";
        protected readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly IHttpClientFactory _httpClientFactory;
        public BaseHttpService(
            IHttpContextAccessor httpContextAccessor,
            IHttpClientFactory httpClientFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// 登录token
        /// </summary>
        protected string Token
        {
            get
            {
                var context = _httpContextAccessor.HttpContext;
                if (context != null)
                {
                    return context.Request.Headers
                      .FirstOrDefault(w => w.Key.Equals(AUTH_KEY))
                      .Value.TryString();
                }
                return "";
            }
        }

        protected LoginUser LoginUser
        {
            get
            {
                var role = _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(e => e.Type == UserClaimType.ROLE);
                var userId = _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(e => e.Type == UserClaimType.USER_ID);
                var name = _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(e => e.Type == UserClaimType.USER_NICKNAME);
                var tenantId = _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(e => e.Type == UserClaimType.TENANT_ID);

                return new LoginUser()
                {
                    Id = userId == null ? Guid.Empty : new Guid(userId.Value),
                    Name = name == null ? "无" : name?.Value,
                    SysTenantId = tenantId == null ? Guid.Empty : new Guid(tenantId?.Value),
                    IsDefault = role == null ? false : role.Value.Equals(UserRoleType.RULER)
                };
            }
        }

        /// <summary>
        /// 获取HttpClient
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected HttpClient GetHttpClient(string name)
        {
            var client = _httpClientFactory.CreateClient(name);
            if (!Token.IsNullOrEmpty())
            {
                client.DefaultRequestHeaders.Add(AUTH_KEY, Token);
            }
            return client;
        }
    }
}

using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http.Formatting;
using Microsoft.AspNetCore.Http;
using OneForAll.Core;
using OneForAll.Core.Extension;
using ScheduleJob.HttpService.Models;
using ScheduleJob.HttpService.Interfaces;
using OneForAll.Core.OAuth;

namespace ScheduleJob.HttpService
{
    /// <summary>
    /// 功能权限校验服务
    /// </summary>
    public class SysPermissionCheckHttpService : BaseHttpService, ISysPermissionCheckHttpService
    {
        private readonly HttpServiceConfig _config;

        public SysPermissionCheckHttpService(
            HttpServiceConfig config,
            IHttpContextAccessor httpContextAccessor,
            IHttpClientFactory httpClientFactory) : base(httpContextAccessor, httpClientFactory)
        {
            _config = config;
        }

        /// <summary>
        /// 验证功能权限
        /// </summary>
        /// <returns>返回消息</returns>
        public async Task<BaseMessage> ValidateAuthorization(string controller, string action)
        {
            if (!Token.IsNullOrEmpty())
            {
                var claims = _httpContextAccessor.HttpContext.User.Claims;
                var uid = claims.FirstOrDefault(e => e.Type == UserClaimType.UserId).Value;

                var client = GetHttpClient(_config.SysBase, Token);
                var postData = new SysPermissionCheckRequest()
                {
                    SysUserId = new Guid(uid),
                    Controller = controller,
                    Action = action
                };
                var result = await client.PostAsync("api/SysPermissionCheck", postData, new JsonMediaTypeFormatter());
                return await result.Content.ReadAsAsync<BaseMessage>();
            }
            return new BaseMessage()
            {
                Status = false,
                ErrType = BaseErrType.TokenInvalid,
                Message = "登录已失效，权限验证失败"
            };
        }
    }
}

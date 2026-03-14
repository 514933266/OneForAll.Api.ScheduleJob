using Microsoft.AspNetCore.Http;
using OneForAll.Core;
using ScheduleJob.HttpService.Interfaces;
using ScheduleJob.HttpService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.HttpService
{
    /// <summary>
    /// 消息通知
    /// </summary>
    public class SysUmsMessageHttpService : BaseHttpService, ISysUmsMessageHttpService
    {
        private readonly HttpServiceConfig _config;

        public SysUmsMessageHttpService(
            HttpServiceConfig config,
            IHttpContextAccessor httpContext,
            IHttpClientFactory httpClientFactory) : base(httpContext, httpClientFactory)
        {
            _config = config;
        }

        /// <summary>
        /// 企业微信机器人通知：Mardown
        /// </summary>
        /// <param name="form">表单</param>
        /// <returns></returns>
        public async Task SendToWechatQyRobotMarkdownAsync(UmsWechatQyRobotTextRequest form)
        {
            var client = GetHttpClient(_config.SysUms);
            if (client != null && client.BaseAddress != null && !string.IsNullOrEmpty(client.BaseAddress.Host))
            {
                await client.PostAsync("/api/WechatQyRobot/Markdown", form, new JsonMediaTypeFormatter());
            }
        }

        /// <summary>
        /// 钉钉机器人通知：Mardown
        /// </summary>
        /// <param name="form">表单</param>
        /// <returns></returns>
        public async Task SendToDingTalkMarkdownAsync(UmsDingTalkRobotMessageRequest form)
        {
            var client = GetHttpClient(_config.SysUms);
            if (client != null && client.BaseAddress != null && !string.IsNullOrEmpty(client.BaseAddress.Host))
            {
                var url = $"{client.BaseAddress}api/DingTalkMessages/Robot/Markdown?isSync=true";
                await client.PostAsync(new Uri(url), form, new JsonMediaTypeFormatter());
            }
        }
    }
}

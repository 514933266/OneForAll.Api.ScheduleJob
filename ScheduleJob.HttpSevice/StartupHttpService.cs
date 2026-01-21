using Microsoft.AspNetCore.Http;
using OneForAll.Core;
using ScheduleJob.HttpService.Interfaces;
using ScheduleJob.HttpService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace ScheduleJob.HttpService
{
    /// <summary>
    /// 启动类请求
    /// </summary>
    public class StartupHttpService : BaseHttpService, IStartupHttpService
    {
        private readonly HttpServiceConfig _config;

        public StartupHttpService(
            HttpServiceConfig config,
            IHttpContextAccessor httpContext,
            IHttpClientFactory httpClientFactory) : base(httpContext, httpClientFactory)
        {
            _config = config;
        }

        /// <summary>
        /// 重新启动（IIS回收时重新发起请求）
        /// </summary>
        /// <returns></returns>
        public async Task<BaseMessage> RequestAsync()
        {
            var client = GetHttpClient(_config.SysJob);
            if (client != null && client.BaseAddress != null)
            {
                var response = await client.GetAsync("/api/Startups");
                var msg = await response.Content.ReadAsAsync<BaseMessage>();
                return msg;
            }
            throw new Exception("未配置项目自我唤醒请求地址");
        }
    }
}

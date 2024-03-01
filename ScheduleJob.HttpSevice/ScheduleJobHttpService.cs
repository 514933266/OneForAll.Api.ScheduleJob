using Microsoft.AspNetCore.Http;
using OneForAll.Core;
using OneForAll.Core.Extension;
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
    /// 定时任务调度中心
    /// </summary>
    public class ScheduleJobHttpService : BaseHttpService, IScheduleJobHttpService
    {
        private readonly HttpServiceConfig _config;

        public ScheduleJobHttpService(
            HttpServiceConfig config,
            IHttpContextAccessor httpContext,
            IHttpClientFactory httpClientFactory) : base(httpContext, httpClientFactory)
        {
            _config = config;
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="request">请求</param>
        /// <returns></returns>
        public async Task<BaseMessage> RegisterAsync(JobRegisterRequest request)
        {
            var client = GetHttpClient(_config.ScheduleJob);
            if (client != null && client.BaseAddress != null)
            {
                var response = await client.PostAsync(client.BaseAddress, request, new JsonMediaTypeFormatter());
                var msg = await response.Content.ReadAsAsync<BaseMessage>();
                return msg;
            }
            return new BaseMessage().Fail("未配置调度中心地址");
        }

        /// <summary>
        /// 定时服务下线
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <returns结果</returns>
        public async Task<BaseMessage> DownLineAsync(string appId, string taskName)
        {
            var client = GetHttpClient(_config.ScheduleJob);
            if (client != null && client.BaseAddress != null)
            {
                var url = $"{client.BaseAddress}/{appId}/{taskName}";
                var response = await client.DeleteAsync(url);
                var msg = await response.Content.ReadAsAsync<BaseMessage>();
                return msg;
            }
            return new BaseMessage().Fail("未配置调度中心地址");
        }

        /// <summary>
        /// 定时服务运行日志
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="log">日志内容</param>
        /// <returns结果</returns>
        public async Task<BaseMessage> LogAsync(string appId, string taskName, string log)
        {
            var client = GetHttpClient(_config.ScheduleJob);
            if (client != null && client.BaseAddress != null)
            {
                var url = $"{client.BaseAddress}/{appId}/{taskName}";
                var response = await client.PostAsync(url, log, new JsonMediaTypeFormatter());
                var msg = await response.Content.ReadAsAsync<BaseMessage>();
                return msg;
            }
            return new BaseMessage().Fail("未配置调度中心地址");
        }

        /// <summary>
        /// 暂停定时服务
        /// </summary>
        /// <param name="taskName">定时任务名称</param>
        /// <returns结果</returns>
        public async Task<BaseMessage> StopAsync(string url, string taskName)
        {
            var client = new HttpClient();
            url = $"{url}/api/Startups/Default/Jobs/{taskName}/Stop";
            var response = await client.PostAsync(url, null);
            var msg = await response.Content.ReadAsAsync<BaseMessage>();
            return msg;
        }

        /// <summary>
        /// 重启定时服务
        /// </summary>
        /// <param name="taskName">定时任务名称</param>
        /// <returns结果</returns>
        public async Task<BaseMessage> ResumeAsync(string url, string taskName)
        {
            var client = new HttpClient();
            url = $"{url}/api/Startups/Default/Jobs/{taskName}/Resume";
            var response = await client.PostAsync(url, null);
            var msg = await response.Content.ReadAsAsync<BaseMessage>();
            return msg;
        }

        /// <summary>
        /// 执行一次定时服务
        /// </summary>
        /// <param name="taskName">定时任务名称</param>
        /// <returns结果</returns>
        public async Task<BaseMessage> ExcuteAsync(string url, string taskName)
        {
            var client = new HttpClient();
            url = $"{url}/api/Startups/Default/Jobs/{taskName}/Excute";
            var response = await client.PostAsync(url, null);
            var msg = await response.Content.ReadAsAsync<BaseMessage>();
            return msg;
        }
    }
}

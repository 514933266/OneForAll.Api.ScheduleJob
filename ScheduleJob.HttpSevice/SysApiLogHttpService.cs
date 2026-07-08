using Microsoft.AspNetCore.Http;
using ScheduleJob.HttpService.Interfaces;
using ScheduleJob.HttpService.Models;
using OneForAll.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using ScheduleJob.Public.Models;

namespace ScheduleJob.HttpService
{
    /// <summary>
    /// Api日志
    /// </summary>
    public class SysApiLogHttpService : BaseHttpService, ISysApiLogHttpService
    {
        private readonly AuthConfig _authConfig;
        private readonly HttpServiceConfig _config;
        private readonly HttpServiceLogConfig _logConfig;

        public SysApiLogHttpService(
            AuthConfig authConfig,
            HttpServiceConfig config,
            HttpServiceLogConfig logConfig,
            IHttpContextAccessor httpContext,
            IHttpClientFactory httpClientFactory) : base(httpContext, httpClientFactory)
        {
            _authConfig = authConfig;
            _config = config;
            _logConfig = logConfig;
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="form">实体</param>
        /// <returns></returns>
        public async Task AddAsync(SysApiLogRequest form)
        {
            // 检查是否启用日志
            if (!_logConfig.ApiLog)
                return;

            try
            {
                form.CreateTime = DateTime.UtcNow;
                form.ModuleCode = _authConfig.ClientCode;
                form.ModuleName = _authConfig.ClientName;

                var client = GetHttpClient(_config.SysLog);
                if (client != null && client.BaseAddress != null)
                {
                    await client.PostAsync("api/SysApiLogs", form, new JsonMediaTypeFormatter());
                }
            }
            catch
            {
                // 忽略异常
            }
        }
    }
}
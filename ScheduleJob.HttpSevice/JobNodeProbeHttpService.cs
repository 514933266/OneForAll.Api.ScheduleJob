using ScheduleJob.HttpService.Interfaces;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ScheduleJob.HttpService
{
    /// <summary>
    /// 任务运行节点存活探测
    /// </summary>
    public class JobNodeProbeHttpService : IJobNodeProbeHttpService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public JobNodeProbeHttpService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// 探测节点是否存活（GET {nodeUrl}/api/Startups 返回 200 视为存活）
        /// </summary>
        /// <param name="nodeUrl">节点地址</param>
        /// <returns>是否存活</returns>
        public async Task<bool> IsAliveAsync(string nodeUrl)
        {
            if (string.IsNullOrWhiteSpace(nodeUrl))
                return false;

            // 无法探测（非http地址）由调用方按存活处理，这里只负责真实请求
            if (!nodeUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                var baseUrl = nodeUrl.TrimEnd('/');
                var client = _httpClientFactory.CreateClient();
                using (var response = await client.GetAsync($"{baseUrl}/api/Startups"))
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch
            {
                // 无响应视为不存活
                return false;
            }
        }
    }
}

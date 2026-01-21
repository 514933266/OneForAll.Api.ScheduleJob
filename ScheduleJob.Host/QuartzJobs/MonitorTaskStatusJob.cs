using OneForAll.Core.Extension;
using Quartz;
using ScheduleJob.Application.Interfaces;
using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Enums;
using ScheduleJob.Domain.Repositorys;
using ScheduleJob.Host.Models;
using ScheduleJob.HttpService.Interfaces;
using ScheduleJob.HttpService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TimeCrontab;

namespace ScheduleJob.Host.QuartzJobs
{
    /// <summary>
    /// 定时任务状态监控
    /// </summary>
    [DisallowConcurrentExecution]
    public class MonitorTaskStatusJob : IJob
    {
        private readonly AuthConfig _config;
        private readonly IScheduleJobService _service;
        private readonly IJobTaskRepository _repository;
        private readonly IJobTaskLogRepository _logRepository;
        private readonly IJobNotificationConfigRepository _notificationRepository;
        private readonly ISysUmsMessageHttpService _umsHttpService;
        private readonly IHttpClientFactory _httpClientFactory;

        public MonitorTaskStatusJob(
            AuthConfig config,
            IScheduleJobService service,
            IJobTaskRepository repository,
            IJobTaskLogRepository logRepository,
            IJobNotificationConfigRepository notificationRepository,
            ISysUmsMessageHttpService umsHttpService,
            IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _service = service;
            _repository = repository;
            _logRepository = logRepository;
            _notificationRepository = notificationRepository;
            _umsHttpService = umsHttpService;
            _httpClientFactory = httpClientFactory;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var now = DateTime.UtcNow;
                var jobs = await _repository.GetListEnabledAsync(asNoTracking: true);
                var errorCount = 0;

                foreach (var job in jobs)
                {
                    try
                    {
                        job.Status = JobTaskStatusEnum.Running;

                        // 1. 定义北京时间时区
                        var beijingTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time"); // 或 "Asia/Shanghai"

                        var nowUtc = DateTime.UtcNow;
                        // 将当前 UTC 时间转为北京时间，用于 Cron 计算
                        var nowBeijing = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, beijingTimeZone);

                        // 2. 解析 Cron（基于北京时间）
                        var crontab = Crontab.Parse(job.Cron, CronStringFormat.WithSeconds);
                        var lastExpectedRunTimeBeijing = crontab.GetPreviousOccurrence(nowBeijing); // 返回的是 DateTime，Kind=Unspecified，但语义是北京时间

                        // 3. 将“北京时间”转换为 UTC 时间（用于和日志时间比较）
                        var lastExpectedRunTimeUtc = TimeZoneInfo.ConvertTimeToUtc(lastExpectedRunTimeBeijing, beijingTimeZone);

                        // 4. 宽限期也基于 UTC
                        var gracePeriodUtc = lastExpectedRunTimeUtc.AddMinutes(5);

                        // 5. 当前 UTC 时间
                        if (nowUtc > gracePeriodUtc)
                        {
                            // 任务可能错过了执行
                            var isAlive = await CheckForSurvivalAsync(job);
                            if (!isAlive)
                            {
                                job.Status = JobTaskStatusEnum.Error;
                                // 注意：通知中可传北京时间，便于理解
                                await SendNotificationAsync(job, lastExpectedRunTimeBeijing);
                                errorCount++;
                                continue;
                            }

                            var latestLog = await _logRepository.GetTop1ByTaskAsync(job.AppId, job.Name);
                            if (latestLog == null || latestLog.CreateTime < lastExpectedRunTimeUtc)
                            {
                                job.Status = JobTaskStatusEnum.Error;
                                await SendNotificationAsync(job, lastExpectedRunTimeBeijing);
                                errorCount++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await AddLogAsync($"巡检定时任务 {job.Name} 时发生异常: {ex.Message}", true);
                        job.Status = JobTaskStatusEnum.Error;
                        errorCount++;
                    }
                }

                var effected = await _repository.SaveChangesAsync();
                await AddLogAsync($"【巡检定时任务】完成，共{jobs.Count()}个任务，发现{errorCount}个异常，数据库影响行数:{effected}");
            }
            catch (Exception ex)
            {
                await AddLogAsync($"【严重】巡检定时任务执行失败： {ex.Message}\r\n{ex.StackTrace}", true);
            }
        }

        // 检查定时任务是否存活（HTTP 探测）
        private async Task<bool> CheckForSurvivalAsync(JobTask job)
        {
            try
            {
                // 无法探测，视为存活
                if (string.IsNullOrEmpty(job.NodeName) || !job.NodeName.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    return true;

                var baseUrl = job.NodeName.TrimEnd('/');
                var url = $"{baseUrl}/api/Startups";

                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(url);

                var isAlive = response.StatusCode == System.Net.HttpStatusCode.OK;
                await AddLogAsync($"主动探测任务 {job.Name} 状态：{job.NodeName} -> {(isAlive ? "运行中" : "已停止")}，最后心跳:{job.HeartbeatTime}");
                return isAlive;
            }
            catch (Exception ex)
            {
                await AddLogAsync($"探测任务 {job.Name} 失败（无响应）：{job.NodeName}，错误:{ex.Message}", true);
                return false;
            }
        }

        // 记录监控日志
        private async Task AddLogAsync(string log, bool isException = false)
        {
            await _service.AddLogAsync(_config.ClientCode, typeof(MonitorTaskStatusJob).Name, log, isException);
        }

        // 发送通知（支持多种类型，当前仅企微机器人）
        private async Task SendNotificationAsync(JobTask job, DateTime expectedRunTime)
        {
            try
            {
                var configs = await _notificationRepository.GetListAsync();
                var webhookUrls = configs
                    .Where(c => c.NotificationType == JobNotificationTypeEnum.WxQtRoot)
                    .SelectMany(c => c.TargetJson.FromJson<List<string>>())
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Distinct()
                    .ToList();

                if (!webhookUrls.Any()) return;

                await SendToWechatQyRobotAsync(job, webhookUrls, expectedRunTime);
            }
            catch (Exception ex)
            {
                await AddLogAsync($"发送通知失败: {ex.Message}", true);
            }
        }

        // 发送企业微信机器人消息（Markdown）
        private async Task SendToWechatQyRobotAsync(JobTask job, List<string> webhookUrls, DateTime expectedRunTime)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## <font color=\"red\">⚠️ 定时任务异常告警</font>  ");
            sb.AppendLine($"**名称**：{job.Name}  ");
            sb.AppendLine($"**运行节点**：{job.NodeName}  ");
            sb.AppendLine($"**节点IP**：{job.IpAddress}  ");
            sb.AppendLine($"**预期执行时间**：{expectedRunTime:yyyy-MM-dd HH:mm:ss}  ");
            sb.AppendLine($"**最后心跳时间**：{job.HeartbeatTime:yyyy-MM-dd HH:mm:ss}  ");
            sb.AppendLine($"**告警时间**：{DateTime.Now:yyyy-MM-dd HH:mm:ss}  ");

            // 使用 Task.WhenAll 确保所有通知都完成
            await Task.WhenAll(webhookUrls.Select(async webhookUrl =>
            {
                try
                {
                    await _umsHttpService.SendToWechatQyRobotMarkdownAsync(new UmsWechatQyRobotTextRequest
                    {
                        WebhookUrl = webhookUrl,
                        Content = sb.ToString()
                    });
                }
                catch (Exception ex)
                {
                    await AddLogAsync($"发送企微机器人消息失败({webhookUrl})：{ex.Message}", true);
                }
            }));
        }
    }
}
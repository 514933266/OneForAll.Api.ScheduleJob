using OneForAll.Core.Extension;
using Quartz;
using ScheduleJob.Application.Interfaces;
using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Enums;
using ScheduleJob.Domain.Repositorys;
using ScheduleJob.Host.Models;
using ScheduleJob.HttpService.Interfaces;
using ScheduleJob.HttpService.Models;
using ScheduleJob.Domain.Interfaces;
using ScheduleJob.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TimeCrontab;
using ScheduleJob.Domain.ValueObjects;
using ScheduleJob.Public.Models;

namespace ScheduleJob.Host.QuartzJobs
{
    /// <summary>
    /// 定时任务状态监控
    /// </summary>
    [DisallowConcurrentExecution]
    public class MonitorTaskStatusJob : BaseLockJob
    {
        private readonly AuthConfig _config;
        private readonly IScheduleJobService _service;
        private readonly IJobTaskRepository _repository;
        private readonly IJobNotificationConfigRepository _notificationRepository;
        private readonly IJobTaskManager _taskManager;
        private readonly IJobTaskLogManager _logManager;
        private readonly ISysUmsMessageHttpService _umsHttpService;
        private readonly IHttpClientFactory _httpClientFactory;

        public MonitorTaskStatusJob(
            AuthConfig config,
            IScheduleJobService service,
            IJobTaskRepository repository,
            IJobRunningLockRepository lockRepository,
            IJobLockHolderRepository holderRepository,
            IJobNotificationConfigRepository notificationRepository,
            IJobTaskManager taskManager,
            IJobTaskLogManager logManager,
            ISysUmsMessageHttpService umsHttpService,
            IHttpClientFactory httpClientFactory)
            : base(config, service, lockRepository, holderRepository)
        {
            _config = config;
            _service = service;
            _repository = repository;
            _notificationRepository = notificationRepository;
            _taskManager = taskManager;
            _logManager = logManager;
            _umsHttpService = umsHttpService;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteInternalAsync(IJobExecutionContext context)
        {
            var jobs = await _repository.GetListEnabledAsync(asNoTracking: true);
            var errorCount = 0;

            // 定义北京时间时区（提到循环外避免重复创建）
            var beijingTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");

            foreach (var job in jobs)
            {
                try
                {
                    if (!job.IsEnabled) continue;

                    job.Status = JobTaskStatusEnum.Running;

                    var nowUtc = DateTime.UtcNow;
                    // 将当前 UTC 时间转为北京时间，用于 Cron 计算
                    var nowBeijing = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, beijingTimeZone);

                    // 解析 Cron（基于北京时间）
                    var crontab = Crontab.Parse(job.Cron, CronStringFormat.WithSeconds);
                    var lastExpectedRunTimeBeijing = crontab.GetPreviousOccurrence(nowBeijing);

                    // 将"北京时间"转换为 UTC 时间（用于和日志时间比较）
                    var lastExpectedRunTimeUtc = TimeZoneInfo.ConvertTimeToUtc(lastExpectedRunTimeBeijing, beijingTimeZone);

                    // 宽限期基于 UTC
                    var gracePeriodUtc = lastExpectedRunTimeUtc.AddMinutes(5);

                    // 当前 UTC 时间是否已超过宽限期
                    if (nowUtc > gracePeriodUtc)
                    {
                        // 任务可能错过了执行
                        var isAlive = await CheckForSurvivalAsync(job, beijingTimeZone);
                        if (!isAlive)
                        {
                            job.Status = JobTaskStatusEnum.Error;
                            // 注意：通知中可传北京时间，便于理解
                            //await SendNotificationAsync(job, lastExpectedRunTimeBeijing, beijingTimeZone);
                            errorCount++;
                            continue;
                        }

                        // 任务存活但最后运行时间早于预期执行时间，说明任务进程在但未按时执行
                        if (job.RunningTime < lastExpectedRunTimeUtc)
                        {
                            job.Status = JobTaskStatusEnum.Error;
                            //await SendNotificationAsync(job, lastExpectedRunTimeBeijing, beijingTimeZone);
                            errorCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    await AddLogAsync($"巡检定时任务状态 {job.Name} 时发生异常: {ex.Message}", true);
                    job.Status = JobTaskStatusEnum.Error;
                    errorCount++;
                }
            }

            var effected = await _repository.SaveChangesAsync();
            await AddLogAsync($"巡检定时任务状态完成，共{jobs.Count()}个任务，发现{errorCount}个异常");
        }

        // 检查定时任务是否存活（HTTP 探测）
        private async Task<bool> CheckForSurvivalAsync(JobTask job, TimeZoneInfo beijingTimeZone)
        {
            try
            {
                // 无法探测，视为存活
                if (string.IsNullOrEmpty(job.NodeName) || !job.NodeName.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    return true;

                var baseUrl = job.NodeName.TrimEnd('/');
                var url = $"{baseUrl}/api/Startups";

                var client = _httpClientFactory.CreateClient();
                using (var response = await client.GetAsync(url))
                {
                    var isAlive = response.StatusCode == System.Net.HttpStatusCode.OK;
                    var heartbeatBeijing = TimeZoneInfo.ConvertTimeFromUtc(job.HeartbeatTime, beijingTimeZone);
                    await AddLogAsync($"主动探测任务 {job.Name} 状态：{job.NodeName} -> {(isAlive ? "运行中" : "已停止")}，最后心跳:{heartbeatBeijing:yyyy-MM-dd HH:mm:ss}");

                    if (isAlive)
                    {
                        // 更新心跳时间
                        await _taskManager.HeartbeatAsync(job.AppId, job.Name);
                        // 写入心跳日志
                        await _logManager.AddAsync(new JobTaskLogForm()
                        {
                            AppId = job.AppId,
                            TaskName = job.Name,
                            Content = $"主动探测存活，运行节点【{job.NodeName}】",
                            Type = JobTaskLogTypeEnum.Heartbeat
                        });
                    }

                    return isAlive;
                }
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

        // 发送通知（支持企微机器人、钉钉机器人）
        private async Task SendNotificationAsync(JobTask job, DateTime expectedRunTimeBeijing, TimeZoneInfo beijingTimeZone)
        {
            try
            {
                var configs = await _notificationRepository.GetListAsync();

                // 企业微信机器人
                var wxWebhookUrls = configs
                    .Where(c => c.NotificationType == JobNotificationTypeEnum.WxQtRoot)
                    .Select(c => c.TargetJson.FromJson<WxQtRobotTargetVo>())
                    .ToList();

                if (wxWebhookUrls.Any())
                {
                    await SendToWechatQyRobotAsync(job, wxWebhookUrls, expectedRunTimeBeijing, beijingTimeZone);
                }

                // 钉钉机器人
                var dingTalkTargets = configs
                    .Where(c => c.NotificationType == JobNotificationTypeEnum.DingTalkRobot)
                    .Select(c => c.TargetJson.FromJson<DingTalkRobotTargetVo>())
                    .ToList();

                if (dingTalkTargets.Any())
                {
                    await SendToDingTalkRobotAsync(job, dingTalkTargets, expectedRunTimeBeijing, beijingTimeZone);
                }
            }
            catch (Exception ex)
            {
                await AddLogAsync($"发送通知失败: {ex.Message}", true);
            }
        }

        // 发送企业微信机器人消息（Markdown）
        private async Task SendToWechatQyRobotAsync(JobTask job, List<WxQtRobotTargetVo> targets, DateTime expectedRunTimeBeijing, TimeZoneInfo beijingTimeZone)
        {
            var heartbeatBeijing = TimeZoneInfo.ConvertTimeFromUtc(job.HeartbeatTime, beijingTimeZone);
            var nowBeijing = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, beijingTimeZone);

            var sb = new StringBuilder();
            sb.AppendLine("## <font color=\"red\">⚠️ 定时任务异常告警</font>  ");
            sb.AppendLine($"**名称**：{job.Name}  ");
            sb.AppendLine($"**客户端Id**：{job.AppId}  ");
            sb.AppendLine($"**所属分组**：{job.GroupName}  ");
            sb.AppendLine($"**运行节点**：{job.NodeName}  ");
            sb.AppendLine($"**节点IP**：{job.IpAddress}  ");
            sb.AppendLine($"**预期执行时间**：{expectedRunTimeBeijing:yyyy-MM-dd HH:mm:ss}  ");
            sb.AppendLine($"**最后心跳时间**：{heartbeatBeijing:yyyy-MM-dd HH:mm:ss}  ");
            sb.AppendLine($"**告警时间**：{nowBeijing:yyyy-MM-dd HH:mm:ss}  ");

            // 使用 Task.WhenAll 确保所有通知都完成
            await Task.WhenAll(targets.Select(async target =>
            {
                try
                {
                    await _umsHttpService.SendToWechatQyRobotMarkdownAsync(new UmsWechatQyRobotTextRequest
                    {
                        WebhookUrl = target.WebhookUrl,
                        Content = sb.ToString()
                    });
                }
                catch (Exception ex)
                {
                    await AddLogAsync($"发送企微机器人消息失败({target.WebhookUrl})：{ex.Message}", true);
                }
            }));
        }

        // 发送钉钉机器人消息（Markdown）
        private async Task SendToDingTalkRobotAsync(JobTask job, List<DingTalkRobotTargetVo> targets, DateTime expectedRunTimeBeijing, TimeZoneInfo beijingTimeZone)
        {
            var heartbeatBeijing = TimeZoneInfo.ConvertTimeFromUtc(job.HeartbeatTime, beijingTimeZone);
            var nowBeijing = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, beijingTimeZone);

            var sb = new StringBuilder();
            sb.AppendLine("## 定时任务异常告警  ");
            sb.AppendLine($"**名称**：{job.Name}  ");
            sb.AppendLine($"**运行节点**：{job.NodeName}  ");
            sb.AppendLine($"**节点IP**：{job.IpAddress}  ");
            sb.AppendLine($"**预期执行时间**：{expectedRunTimeBeijing:yyyy-MM-dd HH:mm:ss}  ");
            sb.AppendLine($"**最后心跳时间**：{heartbeatBeijing:yyyy-MM-dd HH:mm:ss}  ");
            sb.AppendLine($"**告警时间**：{nowBeijing:yyyy-MM-dd HH:mm:ss}  ");

            await Task.WhenAll(targets.Select(async target =>
            {
                try
                {
                    await _umsHttpService.SendToDingTalkMarkdownAsync(new UmsDingTalkRobotMessageRequest
                    {
                        WebhookUrl = target.WebhookUrl,
                        Sign = target.Sign,
                        Title = "定时任务异常告警",
                        Content = sb.ToString(),
                        IsAtAll = false
                    });
                }
                catch (Exception ex)
                {
                    await AddLogAsync($"发送钉钉机器人消息失败({target.WebhookUrl})：{ex.Message}", true);
                }
            }));
        }
    }
}
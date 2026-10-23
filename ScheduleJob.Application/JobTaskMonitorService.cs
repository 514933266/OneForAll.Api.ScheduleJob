using OneForAll.Core.Extension;
using ScheduleJob.Application.Dtos;
using ScheduleJob.Application.Interfaces;
using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Enums;
using ScheduleJob.Domain.Interfaces;
using ScheduleJob.Domain.Models;
using ScheduleJob.Domain.ValueObjects;
using ScheduleJob.HttpService.Interfaces;
using ScheduleJob.HttpService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Application
{
    /// <summary>
    /// 定时任务状态巡检
    /// </summary>
    public class JobTaskMonitorService : IJobTaskMonitorService
    {
        private readonly IJobTaskQueryService _taskQueryService;
        private readonly IJobTaskManager _taskManager;
        private readonly IJobTaskLogManager _logManager;
        private readonly IJobNotificationConfigQueryService _notificationQueryService;
        private readonly IJobNodeProbeHttpService _probeHttpService;
        private readonly ISysUmsMessageHttpService _umsHttpService;

        public JobTaskMonitorService(
            IJobTaskQueryService taskQueryService,
            IJobTaskManager taskManager,
            IJobTaskLogManager logManager,
            IJobNotificationConfigQueryService notificationQueryService,
            IJobNodeProbeHttpService probeHttpService,
            ISysUmsMessageHttpService umsHttpService)
        {
            _taskQueryService = taskQueryService;
            _taskManager = taskManager;
            _logManager = logManager;
            _notificationQueryService = notificationQueryService;
            _probeHttpService = probeHttpService;
            _umsHttpService = umsHttpService;
        }

        /// <summary>
        /// 巡检所有启用中的定时任务：超过预期执行宽限期的主动探测节点存活，异常任务标记为错误状态
        /// </summary>
        /// <returns>巡检结果（含执行日志）</returns>
        public async Task<JobTaskMonitorDto> InspectAsync()
        {
            var dto = new JobTaskMonitorDto();
            var jobs = (await _taskQueryService.GetListEnabledAsync(true))?.ToList() ?? new List<JobTask>();

            dto.TotalCount = jobs.Count;

            // 定义北京时间时区（提到循环外避免重复创建）
            var beijingTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
            var utcNow = DateTime.UtcNow;

            foreach (var job in jobs)
            {
                try
                {
                    if (!job.IsEnabled) continue;

                    // 由 cron 推算最近一次预期执行时间，未过宽限期则跳过
                    var inspect = _taskManager.Inspect(job, utcNow);
                    if (!inspect.IsPastGracePeriod) continue;

                    // 任务可能错过了执行，主动探测节点是否存活
                    var isAlive = await CheckForSurvivalAsync(job, beijingTimeZone, dto.Logs);
                    if (!isAlive || inspect.IsMissed)
                    {
                        await _taskManager.ChangeStatusByIdAsync(job.Id, JobTaskStatusEnum.Error);
                        // 通知发送（保留原有能力，当前未启用）
                        //await SendNotificationAsync(job, inspect.LastExpectedRunTime, beijingTimeZone);
                        dto.ErrorCount++;
                    }
                }
                catch (Exception ex)
                {
                    dto.Logs.Add($"巡检定时任务状态 {job.Name} 时发生异常: {ex.Message}");
                    await _taskManager.ChangeStatusByIdAsync(job.Id, JobTaskStatusEnum.Error);
                    dto.ErrorCount++;
                }
            }

            dto.Logs.Add($"巡检定时任务状态完成，共{dto.TotalCount}个任务，发现{dto.ErrorCount}个异常");
            return dto;
        }

        /// <summary>
        /// 检查定时任务是否存活（HTTP 探测）
        /// </summary>
        /// <param name="job">任务</param>
        /// <param name="beijingTimeZone">北京时间时区</param>
        /// <param name="logs">执行日志</param>
        /// <returns>是否存活</returns>
        private async Task<bool> CheckForSurvivalAsync(JobTask job, TimeZoneInfo beijingTimeZone, List<string> logs)
        {
            // 无法探测（节点地址非http），视为存活
            if (string.IsNullOrEmpty(job.NodeName) || !job.NodeName.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return true;

            try
            {
                var isAlive = await _probeHttpService.IsAliveAsync(job.NodeName);
                var heartbeatBeijing = TimeZoneInfo.ConvertTimeFromUtc(job.HeartbeatTime, beijingTimeZone);
                logs.Add($"主动探测任务 {job.Name} 状态：{job.NodeName} -> {(isAlive ? "运行中" : "已停止")}，最后心跳:{heartbeatBeijing:yyyy-MM-dd HH:mm:ss}");

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
            catch (Exception ex)
            {
                logs.Add($"探测任务 {job.Name} 失败（无响应）：{job.NodeName}，错误:{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送通知（支持企微机器人、钉钉机器人）
        /// </summary>
        /// <param name="job">任务</param>
        /// <param name="expectedRunTime">预期执行时间（UTC）</param>
        /// <param name="beijingTimeZone">北京时间时区</param>
        /// <returns>结果</returns>
        private async Task SendNotificationAsync(JobTask job, DateTime expectedRunTime, TimeZoneInfo beijingTimeZone)
        {
            try
            {
                var configs = (await _notificationQueryService.GetListAsync())?.ToList() ?? new List<JobNotificationConfig>();
                var expectedRunTimeBeijing = TimeZoneInfo.ConvertTimeFromUtc(expectedRunTime, beijingTimeZone);

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
            catch
            {
                // 通知失败不影响巡检结果
            }
        }

        /// <summary>
        /// 发送企业微信机器人消息（Markdown）
        /// </summary>
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
                catch
                {
                    // 单个机器人发送失败不影响其它目标
                }
            }));
        }

        /// <summary>
        /// 发送钉钉机器人消息（Markdown）
        /// </summary>
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
                catch
                {
                    // 单个机器人发送失败不影响其它目标
                }
            }));
        }
    }
}

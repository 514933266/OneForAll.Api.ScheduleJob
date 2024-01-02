using OneForAll.Core.Extension;
using Quartz;
using ScheduleJob.Application.Interfaces;
using ScheduleJob.Domain.AggregateRoots;
using ScheduleJob.Domain.Enums;
using ScheduleJob.Domain.Repositorys;
using ScheduleJob.Host.Models;
using ScheduleJob.HttpService.Interfaces;
using ScheduleJob.HttpService.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TimeCrontab;
using static Azure.Core.HttpHeader;
using static Quartz.Logging.OperationName;

namespace ScheduleJob.Host.QuartzJobs
{
    /// <summary>
    /// 定时任务状态监控
    /// </summary>
    [DisallowConcurrentExecution]
    public class MonitorTaskStatusJob : IJob
    {
        private readonly AuthConfig _config;
        private readonly IJobTaskService _service;
        private readonly IJobTaskRepository _repository;
        private readonly IJobTaskLogRepository _logRepository;
        private readonly IJobNotificationConfigRepository _notificationRepository;
        private readonly ISysUmsMessageHttpService _umsHttpService;

        public MonitorTaskStatusJob(
            AuthConfig config,
            IJobTaskService service,
            IJobTaskRepository repository,
            IJobTaskLogRepository logRepository,
            IJobNotificationConfigRepository notificationRepository,
            ISysUmsMessageHttpService umsHttpService)
        {
            _config = config;
            _service = service;
            _repository = repository;
            _logRepository = logRepository;
            _notificationRepository = notificationRepository;
            _umsHttpService = umsHttpService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            // 超过5分钟未心跳改为异常状态
            var names = new string[] { typeof(MonitorTaskStatusJob).Name, typeof(DeleteTaskLogJob).Name };
            var jobs = await _repository.GetListAsync(w => !names.Contains(w.Name));
            foreach (var job in jobs)
            {
                job.Status = JobTaskStatusEnum.Running;
                if (job.IsEnabled)
                {
                    var fiveTime = DateTime.Now.AddMinutes(-5);
                    var heartbeatTime = job.HeartbeatTime > job.RunningTime ? job.HeartbeatTime : job.RunningTime;
                    var crontab = Crontab.Parse(job.Cron, CronStringFormat.WithSeconds);
                    var nextTime = crontab.GetNextOccurrence(heartbeatTime);

                    var isAlice = await CheckForSurvivalAsync(job, nextTime);
                    if (nextTime < fiveTime)
                    {
                        if (!isAlice)
                        {
                            job.Status = JobTaskStatusEnum.Error;
                            await SendNotificationAsync(job, nextTime);
                        }
                        else
                        {
                            // 检查是否有日志，如果没有则没有运行
                            var log = await _logRepository.GetTop1ByTaskAsync(job.AppId, job.Name);
                            if (log != null)
                            {
                                // 日志小于心跳时间1分钟判定异常
                                if (log.CreateTime < fiveTime && log.CreateTime < heartbeatTime.AddMinutes(1))
                                {
                                    job.Status = JobTaskStatusEnum.Error;
                                    await SendNotificationAsync(job, nextTime);
                                }
                            }
                            else
                            {
                                job.Status = JobTaskStatusEnum.Error;
                                await SendNotificationAsync(job, nextTime);
                            }
                        }
                    }
                }
                else
                {
                    job.Status = JobTaskStatusEnum.Unstart;
                }
            }
            var effected = await _repository.SaveChangesAsync();
            await AddLogAsync($"巡检定时任务状态执行完成，共有{jobs.Count()}个定时任务,异常{effected}个");
        }

        private async Task<bool> CheckForSurvivalAsync(JobTask job, DateTime nextTime)
        {
            if (job.NodeName.StartsWith("http") || job.NodeName.StartsWith("https"))
            {
                var client = new HttpClient();
                var url = $"{job.NodeName}/api/Startups";
                var res = await client.GetAsync(url);
                var result = res.StatusCode == System.Net.HttpStatusCode.OK ? true : false;
                await AddLogAsync($"主动探测定时任务{job.Name}运行状态，节点：{job.NodeName}，Ip：{job.IpAddress}，状态：{(result == true ? "运行中" : "无响应")}，最后心跳时间：{job.HeartbeatTime}");
                return result;
            }
            return true;
        }

        private async Task AddLogAsync(string log)
        {
            await _service.AddLogAsync(_config.ClientCode, typeof(MonitorTaskStatusJob).Name, log);
        }

        private async Task SendNotificationAsync(JobTask job, DateTime nextTime)
        {
            var configs = await _notificationRepository.GetListAsync();
            configs.ForEach(async e =>
            {
                var targets = e.TargetJson.FromJson<List<string>>();
                switch (e.NotificationType)
                {
                    case JobNotificationTypeEnum.WxQtRoot:
                        await SendToWechatQyRobotAsync(job, targets, nextTime);
                        break;
                }
            });
        }

        // 企微机器人消息
        private async Task SendToWechatQyRobotAsync(JobTask job, List<string> webhookUrls, DateTime nextTime)
        {
            var sb = new StringBuilder("## <font color=\"red\">警告：定时任务超时异常</font>  \r\n");
            sb.Append($"名称：{job.Name}  \r\n");
            sb.Append($"运行节点：{job.NodeName}  \r\n");
            sb.Append($"节点Ip：{job.IpAddress}  \r\n");
            sb.Append($"异常摘要：下一次执行时间：{nextTime}，最后心跳时间:{job.HeartbeatTime}  \r\n");
            sb.Append($"发生时间：{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}  \r\n");
            webhookUrls.ForEach(async e =>
            {
                await _umsHttpService.SendToWechatQyRobotMarkdownAsync(new UmsWechatQyRobotTextForm()
                {
                    WebhookUrl = e,
                    Content = sb.ToString()
                });
            });
        }
    }
}

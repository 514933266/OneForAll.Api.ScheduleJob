using Quartz;
using ScheduleJob.Application.Interfaces;
using ScheduleJob.Domain.Interfaces;
using ScheduleJob.Domain.Repositorys;
using ScheduleJob.Host.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using TimeCrontab;
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
        public MonitorTaskStatusJob(
            AuthConfig config,
            IJobTaskService service,
            IJobTaskRepository repository)
        {
            _config = config;
            _service = service;
            _repository = repository;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            // 超过5分钟未心跳改为异常状态
            var jobs = await _repository.GetListAsync();
            foreach (var job in jobs)
            {
                var crontab = Crontab.Parse(job.Cron, CronStringFormat.WithSeconds);
                var nextTime = crontab.GetNextOccurrence(job.RunningTime);
                if (nextTime < DateTime.Now.AddMinutes(-5))
                {
                    job.Status = Domain.Enums.JobTaskStatusEnum.Error;
                }
                else
                {
                    job.Status = Domain.Enums.JobTaskStatusEnum.Running;
                }
            }
            var effected = await _repository.SaveChangesAsync();
            await _service.AddLogAsync(_config.ClientCode, typeof(MonitorTaskStatusJob).Name, $"巡检定时任务状态执行完成，共有{jobs.Count()}个定时任务,异常{effected}个");
        }
    }
}

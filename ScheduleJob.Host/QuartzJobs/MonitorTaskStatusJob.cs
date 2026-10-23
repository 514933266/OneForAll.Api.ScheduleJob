using Quartz;
using ScheduleJob.Application.Interfaces;
using ScheduleJob.Host.Models;
using ScheduleJob.Public.Models;
using System.Threading.Tasks;

namespace ScheduleJob.Host.QuartzJobs
{
    /// <summary>
    /// 定时任务状态监控
    /// </summary>
    [DisallowConcurrentExecution]
    public class MonitorTaskStatusJob : BaseLockJob
    {
        private readonly IJobTaskMonitorService _monitorService;

        public MonitorTaskStatusJob(
            AuthConfig config,
            JobLockConfig jobLockConfig,
            IScheduleJobService service,
            IJobLockService jobLockService,
            IJobMonitorStateService monitorStateService,
            IJobTaskMonitorService monitorService)
            : base(config, jobLockConfig, service, jobLockService, monitorStateService)
        {
            _monitorService = monitorService;
        }

        protected override async Task ExecuteInternalAsync(IJobExecutionContext context)
        {
            var result = await _monitorService.InspectAsync();
            foreach (var log in result.Logs)
            {
                await AddLogAsync(log, log.Contains("异常:"));
            }
        }
    }
}

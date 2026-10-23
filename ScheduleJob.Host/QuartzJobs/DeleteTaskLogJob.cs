using OneForAll.Core.Extension;
using Quartz;
using ScheduleJob.Application.Interfaces;
using ScheduleJob.Domain.Models;
using ScheduleJob.Host.Models;
using ScheduleJob.Public.Models;
using System.Threading.Tasks;

namespace ScheduleJob.Host.QuartzJobs
{
    /// <summary>
    /// 定时任务日志清理
    /// </summary>
    [DisallowConcurrentExecution]
    public class DeleteTaskLogJob : BaseLockJob
    {
        private readonly IJobTaskLogService _logService;

        public DeleteTaskLogJob(
            AuthConfig config,
            JobLockConfig jobLockConfig,
            IScheduleJobService service,
            IJobLockService jobLockService,
            IJobMonitorStateService monitorStateService,
            IJobTaskLogService logService)
            : base(config, jobLockConfig, service, jobLockService, monitorStateService)
        {
            _logService = logService;
        }

        protected override async Task ExecuteInternalAsync(IJobExecutionContext context)
        {
            // 从 JobDataMap 中读取配置，未配置则使用默认值
            var config = GetLogConfig(context);

            var result = await _logService.ClearExpiredAsync(config);
            foreach (var log in result.Logs)
            {
                await AddLogAsync(log);
            }
        }

        /// <summary>
        /// 从 JobDataMap 读取日志清理配置
        /// </summary>
        private JobTaskLogCleanupConfig GetLogConfig(IJobExecutionContext context)
        {
            var dataJson = context.JobDetail.JobDataMap.GetString("Data");
            if (!string.IsNullOrWhiteSpace(dataJson))
            {
                var config = dataJson.FromJson<JobTaskLogCleanupConfig>();
                if (config != null)
                    return config;
            }
            return new JobTaskLogCleanupConfig();
        }
    }
}

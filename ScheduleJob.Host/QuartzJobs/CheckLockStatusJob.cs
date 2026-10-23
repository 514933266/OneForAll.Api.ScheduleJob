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
    /// 定时任务锁状态检查（定期释放超过执行时长的残留锁）
    /// 锁超时阈值 = 任务执行周期（cron 相邻触发间隔） + 缓冲时间，并限制在 [最低, 最高] 区间内；
    /// 继承 BaseLockJob 参与并发控制，并将默认最大并发数放宽（避免多次被强杀后残留锁计数把本任务自身卡死）；
    /// 通过实体操作删除超时记录并按实际持有数修正计数，多次执行幂等
    /// </summary>
    [DisallowConcurrentExecution]
    public class CheckLockStatusJob : BaseLockJob
    {
        private readonly IJobLockService _jobLockService;

        /// <summary>
        /// 放宽最大并发数：残留锁计数不会把本任务自身卡死
        /// </summary>
        protected override int DefaultMaxConcurrent => 10;

        public CheckLockStatusJob(
            AuthConfig config,
            JobLockConfig jobLockConfig,
            IScheduleJobService service,
            IJobLockService jobLockService,
            IJobMonitorStateService monitorStateService)
            : base(config, jobLockConfig, service, jobLockService, monitorStateService)
        {
            _jobLockService = jobLockService;
        }

        /// <summary>
        /// 实际执行逻辑：按任务执行周期计算各自阈值，删除超时残留锁并修正运行计数
        /// </summary>
        protected override async Task ExecuteInternalAsync(IJobExecutionContext context)
        {
            var config = GetLockConfig(context);
            var result = await _jobLockService.CleanupExpiredAsync(config);

            foreach (var log in result.Logs)
            {
                await AddLogAsync(log);
            }
        }

        /// <summary>
        /// 从 JobDataMap 读取锁状态检查配置
        /// </summary>
        private JobLockCleanupConfig GetLockConfig(IJobExecutionContext context)
        {
            var dataJson = context.JobDetail.JobDataMap.GetString("Data");
            if (!string.IsNullOrWhiteSpace(dataJson))
            {
                var config = dataJson.FromJson<JobLockCleanupConfig>();
                if (config != null)
                    return config;
            }
            return new JobLockCleanupConfig();
        }
    }
}

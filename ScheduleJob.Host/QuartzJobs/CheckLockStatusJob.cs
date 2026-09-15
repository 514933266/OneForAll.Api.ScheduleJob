using OneForAll.Core.Extension;
using Quartz;
using ScheduleJob.Application.Interfaces;
using ScheduleJob.Domain.Repositorys;
using ScheduleJob.Host.Models;
using ScheduleJob.Public.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using TimeCrontab;

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
        private static readonly TimeZoneInfo _chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");

        private readonly IJobRunningLockRepository _lockRepository;
        private readonly IJobLockHolderRepository _holderRepository;
        private readonly IJobTaskRepository _taskRepository;

        /// <summary>
        /// 放宽最大并发数：残留锁计数不会把本任务自身卡死
        /// </summary>
        protected override int DefaultMaxConcurrent => 10;

        public CheckLockStatusJob(
            AuthConfig config,
            IScheduleJobService service,
            IJobRunningLockRepository lockRepository,
            IJobLockHolderRepository holderRepository,
            IJobTaskRepository taskRepository)
            : base(config, service, lockRepository, holderRepository)
        {
            _lockRepository = lockRepository;
            _holderRepository = holderRepository;
            _taskRepository = taskRepository;
        }

        /// <summary>
        /// 实际执行逻辑：按任务执行周期计算各自阈值，删除超时残留锁并修正运行计数
        /// </summary>
        protected override async Task ExecuteInternalAsync(IJobExecutionContext context)
        {
            var lockConfig = GetLockConfig(context);

            // 配置规范化，防止非法配置导致误删正在运行的锁
            var minMinutes = lockConfig.MinExpireMinutes > 0 ? lockConfig.MinExpireMinutes : 30;
            var maxMinutes = lockConfig.MaxExpireMinutes >= minMinutes ? lockConfig.MaxExpireMinutes : Math.Max(360, minMinutes);
            var bufferMinutes = lockConfig.PeriodBufferMinutes >= 0 ? lockConfig.PeriodBufferMinutes : 60;

            var now = DateTime.UtcNow;

            // 1. 按任务的 cron 执行周期计算各自的锁超时时间点（同名任务取最大阈值，保守处理）
            var tasks = await _taskRepository.GetListAllAsync(true);
            var taskExpires = tasks
                .GroupBy(w => w.Name)
                .ToDictionary(
                    g => g.Key,
                    g => now.AddMinutes(-g.Max(w => CalcThresholdMinutes(w.Cron, minMinutes, maxMinutes, bufferMinutes))));

            // 未注册任务（任务表中不存在）的兜底超时时间点（最高阈值）
            var unknownTaskExpireBefore = now.AddMinutes(-maxMinutes);

            // 2. 查询全部锁持有者记录，筛选出超时的残留锁
            var holders = (await _holderRepository.GetListAsync()).ToList();
            var expiredHolders = holders
                .Where(w => w.LockTime < (taskExpires.TryGetValue(w.TaskName, out var expireBefore) ? expireBefore : unknownTaskExpireBefore))
                .ToList();

            // 3. 批量删除超时的残留锁记录
            var deletedHolders = expiredHolders.Count > 0
                ? await _holderRepository.BulkDeleteAsync(expiredHolders)
                : 0;

            // 4. 按剩余持有者数量修正运行计数（只更新不一致的记录，不主动修改 Version，避免干扰获取锁时的乐观锁）
            var holderCounts = holders
                .Except(expiredHolders)
                .GroupBy(w => (w.ClientId, w.TaskName))
                .ToDictionary(g => g.Key, g => g.Count());

            var adjustedLocks = 0;
            var runningLocks = await _lockRepository.GetListAsync();
            foreach (var item in runningLocks)
            {
                var expectedCount = holderCounts.TryGetValue((item.ClientId, item.TaskName), out var count) ? count : 0;
                if (item.CurrentRunningCount != expectedCount)
                {
                    item.CurrentRunningCount = expectedCount;
                    item.UpdateTime = DateTime.UtcNow;
                    await _lockRepository.UpdateAsync(item);
                    adjustedLocks++;
                }
            }

            await AddLogAsync($"锁状态检查完成：共校验 {taskExpires.Count} 个任务阈值（最低{minMinutes}分钟/周期+{bufferMinutes}分钟/最高{maxMinutes}分钟），释放 {deletedHolders} 条残留锁，修正 {adjustedLocks} 条运行计数");
        }

        /// <summary>
        /// 计算任务锁超时阈值（分钟）= 执行周期 + 缓冲，并限制在 [最低, 最高] 区间内
        /// </summary>
        /// <param name="cron">任务 cron 表达式</param>
        /// <param name="minMinutes">最低阈值分钟数</param>
        /// <param name="maxMinutes">最高阈值分钟数</param>
        /// <param name="bufferMinutes">周期附加缓冲分钟数</param>
        /// <returns>超时阈值分钟数</returns>
        private int CalcThresholdMinutes(string cron, int minMinutes, int maxMinutes, int bufferMinutes)
        {
            try
            {
                var crontab = Crontab.Parse(cron, CronStringFormat.WithSeconds);

                // 与任务触发器保持一致，按北京时间计算相邻两次触发间隔
                var nowBeijing = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _chinaTimeZone);
                var next1 = crontab.GetNextOccurrence(nowBeijing);
                var next2 = crontab.GetNextOccurrence(next1);
                var periodMinutes = (next2 - next1).TotalMinutes;

                return (int)Math.Clamp(periodMinutes + bufferMinutes, minMinutes, maxMinutes);
            }
            catch
            {
                // cron 解析失败时按最高阈值兜底，避免误删正在运行的锁
                return maxMinutes;
            }
        }

        /// <summary>
        /// 从 JobDataMap 读取锁状态检查配置
        /// </summary>
        private CheckLockStatusConfig GetLockConfig(IJobExecutionContext context)
        {
            var dataJson = context.JobDetail.JobDataMap.GetString("Data");
            if (!string.IsNullOrWhiteSpace(dataJson))
            {
                var config = dataJson.FromJson<CheckLockStatusConfig>();
                if (config != null)
                    return config;
            }
            return new CheckLockStatusConfig();
        }
    }
}

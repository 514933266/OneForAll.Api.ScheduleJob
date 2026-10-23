using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Interfaces;
using ScheduleJob.Domain.Models;
using ScheduleJob.Domain.Repositorys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeCrontab;

namespace ScheduleJob.Domain.Managers
{
    /// <summary>
    /// 定时任务运行锁
    /// </summary>
    public class JobLockManager : ScheduleJobDomainManager, IJobLockManager
    {
        /// <summary>
        /// 默认最大并发数（仅锁记录首次创建时生效）
        /// </summary>
        private const int DEFAULT_MAX_CONCURRENT = 1;

        private static readonly TimeZoneInfo _chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");

        private readonly IJobRunningLockRepository _lockRepository;
        private readonly IJobLockHolderRepository _holderRepository;
        private readonly IJobTaskRepository _taskRepository;

        public JobLockManager(
            ICurrentUser currentUser,
            IJobRunningLockRepository lockRepository,
            IJobLockHolderRepository holderRepository,
            IJobTaskRepository taskRepository) : base(currentUser)
        {
            _lockRepository = lockRepository;
            _holderRepository = holderRepository;
            _taskRepository = taskRepository;
        }

        /// <summary>
        /// 尝试获取执行资格（锁记录不存在时按传入上限自动创建默认配置）
        /// </summary>
        /// <param name="clientId">客户端id</param>
        /// <param name="clientCode">客户端编号</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="maxConcurrent">最大并发数（仅锁记录首次创建时生效）</param>
        /// <returns>检查结果</returns>
        public async Task<JobLockCheckResult> AcquireAsync(string clientId, string clientCode, string taskName, int maxConcurrent)
        {
            var currentVersion = 0;
            try
            {
                var lockEntity = await _lockRepository.GetWithLockAsync(clientId, taskName, asNoTracking: true);

                // 如果不存在锁配置，创建默认配置
                if (lockEntity == null)
                {
                    lockEntity = new JobRunningLock
                    {
                        Id = Guid.NewGuid(),
                        IsEnabled = true,
                        TaskName = taskName,
                        ClientId = clientId,
                        Version = 0,
                        MaxConcurrent = maxConcurrent > 0 ? maxConcurrent : DEFAULT_MAX_CONCURRENT,
                        CurrentRunningCount = 0,
                        CreateTime = DateTime.UtcNow,
                        UpdateTime = DateTime.UtcNow
                    };

                    _lockRepository.Add(lockEntity);
                }

                currentVersion = lockEntity.Version + 1;

                // 如果未启用并发控制，直接允许执行（未加锁，无需释放）
                if (!lockEntity.IsEnabled)
                    return new JobLockCheckResult { CanExecute = true, LockAcquired = false, Version = currentVersion };

                // 检查是否达到最大并发数
                if (lockEntity.CurrentRunningCount >= lockEntity.MaxConcurrent)
                    return new JobLockCheckResult { CanExecute = false, LockAcquired = false, Version = currentVersion };

                // 事务内：INSERT Holder（利用唯一索引作为锁）+ UPDATE 计数（乐观锁二次校验）
                // 唯一索引冲突已在仓储层转换为 false（并发竞争失败），无需在上层识别
                var isSuccess = await _lockRepository.TryAcquireWithHolderAsync(
                    lockEntity.Id, clientId, clientCode, taskName, lockEntity.Version, currentVersion);

                return new JobLockCheckResult { CanExecute = isSuccess, LockAcquired = isSuccess, Version = currentVersion };
            }
            catch (Exception ex)
            {
                // 锁基础设施异常（锁表未创建、连接失败、超时、死锁等）由调用方决定放行或跳过
                return new JobLockCheckResult
                {
                    CanExecute = false,
                    LockAcquired = false,
                    Version = currentVersion,
                    IsInfrastructureError = true,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 释放执行资格（运行计数减一 + 删除持锁记录）
        /// </summary>
        /// <param name="clientId">客户端id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="version">锁版本号</param>
        /// <returns>是否成功</returns>
        public async Task<bool> ReleaseAsync(string clientId, string taskName, int version)
        {
            var isSuccess = true;

            var lockEntity = await _lockRepository.GetAsync(clientId, taskName, asNoTracking: true);
            if (lockEntity != null && lockEntity.CurrentRunningCount >= 1)
            {
                isSuccess = await _lockRepository.DecrementRunningCountAsync(lockEntity.Id);
            }

            // 删除锁持有者记录（按持有版本精确匹配）
            var holder = await _holderRepository.GetAsync(x => x.ClientId == clientId && x.TaskName == taskName && x.Version == version);
            if (holder != null)
            {
                await _holderRepository.DeleteAsync(holder);
            }

            return isSuccess;
        }

        /// <summary>
        /// 清理超过执行周期的残留锁，并按实际持有数修正运行计数
        /// </summary>
        /// <param name="config">检查配置</param>
        /// <returns>检查结果</returns>
        public async Task<JobLockCleanupResult> CleanupExpiredAsync(JobLockCleanupConfig config)
        {
            var result = new JobLockCleanupResult();
            config = config ?? new JobLockCleanupConfig();

            // 配置规范化，防止非法配置导致误删正在运行的锁
            var minMinutes = config.MinExpireMinutes > 0 ? config.MinExpireMinutes : 30;
            var maxMinutes = config.MaxExpireMinutes >= minMinutes ? config.MaxExpireMinutes : Math.Max(360, minMinutes);
            var bufferMinutes = config.PeriodBufferMinutes >= 0 ? config.PeriodBufferMinutes : 60;

            result.MinMinutes = minMinutes;
            result.MaxMinutes = maxMinutes;
            result.BufferMinutes = bufferMinutes;

            var now = DateTime.UtcNow;

            // 1. 按任务的 cron 执行周期计算各自的锁超时时间点（同名任务取最大阈值，保守处理）
            var tasks = await _taskRepository.GetListAllAsync(true) ?? new List<JobTask>();
            var taskExpires = tasks
                .GroupBy(w => w.Name)
                .ToDictionary(
                    g => g.Key,
                    g => now.AddMinutes(-g.Max(w => CalcThresholdMinutes(w.Cron, minMinutes, maxMinutes, bufferMinutes))));

            // 未注册任务（任务表中不存在）的兜底超时时间点（最高阈值）
            var unknownTaskExpireBefore = now.AddMinutes(-maxMinutes);

            // 2. 查询全部锁持有者记录，筛选出超时的残留锁
            var holders = (await _holderRepository.GetListAsync())?.ToList() ?? new List<JobLockHolder>();
            var expiredHolders = holders
                .Where(w => w.LockTime < (taskExpires.TryGetValue(w.TaskName, out var expireBefore) ? expireBefore : unknownTaskExpireBefore))
                .ToList();

            // 3. 批量删除超时的残留锁记录
            result.DeletedHolderCount = expiredHolders.Count > 0
                ? await _holderRepository.BulkDeleteAsync(expiredHolders)
                : 0;

            // 4. 按剩余持有者数量修正运行计数（只更新不一致的记录，不主动修改 Version，避免干扰获取锁时的乐观锁）
            var holderCounts = holders
                .Except(expiredHolders)
                .GroupBy(w => (w.ClientId, w.TaskName))
                .ToDictionary(g => g.Key, g => g.Count());

            var runningLocks = await _lockRepository.GetListAsync() ?? new List<JobRunningLock>();
            foreach (var item in runningLocks)
            {
                var expectedCount = holderCounts.TryGetValue((item.ClientId, item.TaskName), out var count) ? count : 0;
                if (item.CurrentRunningCount != expectedCount)
                {
                    item.CurrentRunningCount = expectedCount;
                    item.UpdateTime = DateTime.UtcNow;
                    await _lockRepository.UpdateAsync(item);
                    result.AdjustedLockCount++;
                }
            }

            result.TaskThresholdCount = taskExpires.Count;
            return result;
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
    }
}

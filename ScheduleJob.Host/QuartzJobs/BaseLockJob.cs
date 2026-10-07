using Microsoft.Data.SqlClient;
using Quartz;
using ScheduleJob.Application.Interfaces;
using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Repositorys;
using ScheduleJob.Host.Models;
using ScheduleJob.Public.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleJob.Host.QuartzJobs
{
    /// <summary>
    /// 定时任务锁基类 - 在 BaseJob 基础上自动处理并发控制
    /// </summary>
    public abstract class BaseLockJob : BaseJob
    {
        private readonly IJobRunningLockRepository _lockRepository;
        private readonly IJobLockHolderRepository _holderRepository;
        private readonly JobLockConfig _lockConfig;

        protected BaseLockJob(
            AuthConfig authConfig,
            JobLockConfig lockConfig,
            IScheduleJobService scheduleJobService,
            IJobRunningLockRepository lockRepository,
            IJobLockHolderRepository holderRepository,
            IJobMonitorStateRepository stateRepository)
            : base(authConfig, scheduleJobService, stateRepository)
        {
            _lockConfig = lockConfig;
            _lockRepository = lockRepository;
            _holderRepository = holderRepository;
        }

        /// <summary>
        /// Quartz 执行入口（叠加并发控制）
        /// </summary>
        public override async Task Execute(IJobExecutionContext context)
        {
            var taskName = GetTaskName();
            var clientId = _authConfig?.ClientId ?? "未配置ClientId";
            var clientCode = _authConfig?.ClientCode ?? "未配置ClientCode";

            // 尝试获取执行权限（业务逻辑实现在基类）
            var (canExecute, version, lockAcquired) = await TryAcquireLockAsync(clientId, clientCode, taskName);

            if (!canExecute)
            {
                // 已达到最大并发数，跳过执行
                await LogSkipExecutionAsync(context, taskName);
                return;
            }

            try
            {
                // 执行实际业务逻辑（异常捕获由基类 BaseJob 处理）
                await base.Execute(context);
            }
            finally
            {
                // 仅当本次真正持有锁时才需要释放（未启用并发控制或锁异常放行时未参与并发控制）
                if (lockAcquired)
                {
                    // 延迟3秒后再释放锁，防止任务刚执行完立即再次触发
                    await Task.Delay(3000);

                    // 确保释放锁
                    var canRemove = await ReleaseLockAsync(clientId, taskName, version);
                    if (canRemove)
                    {
                        // 删除锁持有者记录
                        await RemoveLockHolderAsync(clientId, taskName, version);
                    }
                }
            }
        }

        /// <summary>
        /// 默认最大并发数（仅锁记录首次创建时生效，子类可覆盖以放宽并发上限）
        /// </summary>
        protected virtual int DefaultMaxConcurrent => 1;

        /// <summary>
        /// 尝试获取执行权限
        /// </summary>
        /// <param name="clientId">客户端Id</param>
        /// <param name="clientCode">客户端编号</param>
        /// <param name="taskName">定时任务名称</param>
        /// <returns>canExecute：是否允许执行；version：当前版本号；lockAcquired：是否已真正持有锁（执行完需释放）</returns>
        private async Task<(bool canExecute, int version, bool lockAcquired)> TryAcquireLockAsync(string clientId, string clientCode, string taskName)
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
                        IsEnabled = true,
                        TaskName = taskName,
                        ClientId = clientId,
                        Version = 0,
                        MaxConcurrent = DefaultMaxConcurrent,
                        CurrentRunningCount = 0,
                        CreateTime = DateTime.UtcNow,
                        UpdateTime = DateTime.UtcNow
                    };

                    var effected = _lockRepository.Add(lockEntity);
                }

                currentVersion = lockEntity.Version + 1;
                // 如果未启用并发控制，直接允许执行（未加锁，无需释放）
                if (!lockEntity.IsEnabled)
                    return (true, currentVersion, false);

                // 检查是否达到最大并发数
                if (lockEntity.CurrentRunningCount >= lockEntity.MaxConcurrent)
                    return (false, currentVersion, false);

                // 事务内：INSERT Holder（利用唯一索引作为锁）+ UPDATE 计数（乐观锁二次校验）
                var isSuccess = await _lockRepository.TryAcquireWithHolderAsync(
                    lockEntity.Id, clientId, clientCode, taskName, lockEntity.Version, currentVersion);

                return (isSuccess, currentVersion, isSuccess);
            }
            catch (Exception ex)
            {
                // 唯一索引冲突属于正常并发竞争（如补建锁记录瞬间被其他实例抢占），拒绝执行
                if (IsUniqueIndexConflict(ex))
                    return (false, currentVersion, false);

                // 锁基础设施异常（锁表未创建、连接失败、超时、死锁等）：
                // 启用 FailOpenOnException 时放行执行（本次不参与并发控制），否则跳过
                if (_lockConfig?.FailOpenOnException == true)
                {
                    await TryLogFailOpenAsync(taskName, ex);
                    return (true, currentVersion, false);
                }

                return (false, currentVersion, false);
            }
        }

        /// <summary>
        /// 判断异常链中是否包含唯一索引冲突（SQL Server 错误号 2601/2627）
        /// </summary>
        private static bool IsUniqueIndexConflict(Exception ex)
        {
            while (ex != null)
            {
                if (ex is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
                    return true;
                ex = ex.InnerException;
            }
            return false;
        }

        /// <summary>
        /// 记录锁异常放行日志（日志记录失败时忽略，确保放行不被阻断）
        /// </summary>
        private async Task TryLogFailOpenAsync(string taskName, Exception ex)
        {
            try
            {
                await AddLogAsync($"任务 [{taskName}] 锁基础设施异常，已放行执行（本次未参与并发控制）: {ex.Message}");
            }
            catch
            {
                // 日志记录失败不影响放行
            }
        }

        /// <summary>
        /// 释放执行权限（减少计数）
        /// </summary>
        private async Task<bool> ReleaseLockAsync(string clientId, string taskName, int version)
        {
            var lockEntity = await _lockRepository.GetAsync(clientId, taskName, asNoTracking: true);

            if (lockEntity != null && lockEntity.CurrentRunningCount >= 1)
            {
                return await _lockRepository.DecrementRunningCountAsync(lockEntity.Id);
            }

            return true;
        }

        /// <summary>
        /// 记录跳过执行的日志
        /// </summary>
        protected virtual async Task LogSkipExecutionAsync(IJobExecutionContext context, string taskName)
        {
            await AddLogAsync($"任务 [{taskName}] 已达到最大并发数，本次执行被跳过");
        }

        /// <summary>
        /// 删除锁持有者记录
        /// </summary>
        private async Task RemoveLockHolderAsync(string clientId, string taskName, int version)
        {
            var holder = await _holderRepository.GetAsync(x => x.ClientId == clientId && x.TaskName == taskName && x.Version == version);

            if (holder != null)
            {
                await _holderRepository.DeleteAsync(holder);
            }
        }
    }
}

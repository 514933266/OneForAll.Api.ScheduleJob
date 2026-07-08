using Quartz;
using ScheduleJob.Application.Interfaces;
using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Repositorys;
using ScheduleJob.Public.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleJob.Host.QuartzJobs
{
    /// <summary>
    /// 定时任务基类 - 自动处理并发控制
    /// </summary>
    public abstract class BaseLockJob : IJob
    {
        private readonly AuthConfig _authConfig;
        private readonly IScheduleJobService _scheduleJobService;
        private readonly IJobRunningLockRepository _lockRepository;
        private readonly IJobLockHolderRepository _holderRepository;

        protected BaseLockJob(
            AuthConfig authConfig,
            IScheduleJobService scheduleJobService,
            IJobRunningLockRepository lockRepository,
            IJobLockHolderRepository holderRepository)
        {
            _authConfig = authConfig;
            _scheduleJobService = scheduleJobService;
            _lockRepository = lockRepository;
            _holderRepository = holderRepository;
        }

        /// <summary>
        /// Quartz 执行入口
        /// </summary>
        public async Task Execute(IJobExecutionContext context)
        {
            var taskName = GetTaskName();
            var clientId = _authConfig?.ClientId ?? "未配置ClientId";
            var clientCode = _authConfig?.ClientCode ?? "未配置ClientCode";

            // 尝试获取执行权限（业务逻辑实现在基类）
            var (canExecute, version) = await TryAcquireLockAsync(clientId, clientCode, taskName);

            if (!canExecute)
            {
                // 已达到最大并发数，跳过执行
                await LogSkipExecutionAsync(context, taskName);
                return;
            }

            try
            {
                // 执行实际业务逻辑
                await ExecuteInternalAsync(context);
            }
            catch (Exception ex)
            {
                // 记录异常
                await LogExceptionAsync(context, taskName, ex);
            }
            finally
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

        /// <summary>
        /// 子类必须实现的实际执行逻辑
        /// </summary>
        protected abstract Task ExecuteInternalAsync(IJobExecutionContext context);

        /// <summary>
        /// 获取任务名称（默认为类名）
        /// </summary>
        protected virtual string GetTaskName()
        {
            return GetType().Name;
        }

        /// <summary>
        /// 尝试获取执行权限
        /// </summary>
        /// <param name="clientId">客户端Id</param>
        /// <param name="clientCode">客户端编号</param>
        /// <param name="taskName">定时任务名称</param>
        /// <returns></returns>
        private async Task<(bool, int)> TryAcquireLockAsync(string clientId, string clientCode, string taskName)
        {
            var currentVersion = 0;
            try
            {
                var lockEntity = await _lockRepository.GetAsync(clientId, taskName, asNoTracking: true);

                // 如果不存在锁配置，创建默认配置（允许1个并发）
                if (lockEntity == null)
                {
                    lockEntity = new JobRunningLock
                    {
                        IsEnabled = true,
                        TaskName = taskName,
                        ClientId = clientId,
                        Version = 0,
                        MaxConcurrent = 1,
                        CurrentRunningCount = 0,
                        CreateTime = DateTime.UtcNow,
                        UpdateTime = DateTime.UtcNow
                    };

                    var effected = _lockRepository.Add(lockEntity);
                }

                currentVersion = lockEntity.Version + 1;
                // 如果未启用并发控制，直接允许执行
                if (!lockEntity.IsEnabled)
                    return (true, currentVersion);

                // 检查是否达到最大并发数
                if (lockEntity.CurrentRunningCount >= lockEntity.MaxConcurrent)
                    return (false, currentVersion);

                // 事务内：INSERT Holder（利用唯一索引作为锁）+ UPDATE 计数（乐观锁二次校验）
                var isSuccess = await _lockRepository.TryAcquireWithHolderAsync(
                    lockEntity.Id, clientId, clientCode, taskName, lockEntity.Version, currentVersion);

                return (isSuccess, currentVersion);
            }
            catch
            {
                // 并发冲突时返回 false
                return (false, currentVersion);
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
            if (_scheduleJobService != null)
            {
                await _scheduleJobService.AddLogAsync(
                    _authConfig?.ClientCode ?? "",
                    taskName,
                    $"任务 [{taskName}] 已达到最大并发数，本次执行被跳过",
                    false);
            }
        }

        /// <summary>
        /// 记录异常日志
        /// </summary>
        protected virtual async Task LogExceptionAsync(IJobExecutionContext context, string taskName, Exception ex)
        {
            if (_scheduleJobService != null)
            {
                await _scheduleJobService.AddLogAsync(
                    _authConfig?.ClientCode ?? "",
                    taskName,
                    $"任务 [{taskName}] 执行异常: {ex.Message}\r\n{ex.StackTrace}",
                    true);
            }
        }

        /// <summary>
        /// 记录日志（子类可直接调用）
        /// </summary>
        /// <param name="log">日志内容</param>
        /// <param name="isException">是否为异常日志</param>
        protected async Task AddLogAsync(string log, bool isException = false)
        {
            if (_scheduleJobService != null)
            {
                await _scheduleJobService.AddLogAsync(
                    _authConfig?.ClientCode ?? "",
                    GetTaskName(),
                    log,
                    isException);
            }
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

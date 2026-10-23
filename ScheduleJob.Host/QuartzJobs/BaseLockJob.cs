using Quartz;
using ScheduleJob.Application.Interfaces;
using ScheduleJob.Host.Models;
using ScheduleJob.Public.Models;
using System;
using System.Threading.Tasks;

namespace ScheduleJob.Host.QuartzJobs
{
    /// <summary>
    /// 定时任务锁基类 - 在 BaseJob 基础上自动处理并发控制
    /// </summary>
    public abstract class BaseLockJob : BaseJob
    {
        private readonly IJobLockService _jobLockService;
        private readonly JobLockConfig _lockConfig;

        protected BaseLockJob(
            AuthConfig authConfig,
            JobLockConfig lockConfig,
            IScheduleJobService scheduleJobService,
            IJobLockService jobLockService,
            IJobMonitorStateService monitorStateService)
            : base(authConfig, scheduleJobService, monitorStateService)
        {
            _lockConfig = lockConfig;
            _jobLockService = jobLockService;
        }

        /// <summary>
        /// Quartz 执行入口（叠加并发控制）
        /// </summary>
        public override async Task Execute(IJobExecutionContext context)
        {
            var taskName = GetTaskName();
            var clientId = _authConfig?.ClientId ?? "未配置ClientId";
            var clientCode = _authConfig?.ClientCode ?? "未配置ClientCode";

            // 尝试获取执行权限（并发控制规则由应用层统一提供）
            var result = await _jobLockService.TryAcquireAsync(clientId, taskName, clientCode, DefaultMaxConcurrent);

            // 锁基础设施异常（锁表未创建、连接失败、超时、死锁等）：
            // 启用 FailOpenOnException 时放行执行（本次不参与并发控制），否则跳过
            if (result.IsInfrastructureError && _lockConfig?.FailOpenOnException == true)
            {
                await TryLogFailOpenAsync(taskName, result.ErrorMessage);
            }
            else if (!result.CanExecute)
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
                if (result.LockAcquired)
                {
                    // 延迟3秒后再释放锁，防止任务刚执行完立即再次触发
                    await Task.Delay(3000);

                    // 释放锁（运行计数减一 + 删除持锁记录）
                    await _jobLockService.ReleaseAsync(clientId, taskName, result.Version);
                }
            }
        }

        /// <summary>
        /// 默认最大并发数（仅锁记录首次创建时生效，子类可覆盖以放宽并发上限）
        /// </summary>
        protected virtual int DefaultMaxConcurrent => 1;

        /// <summary>
        /// 记录锁异常放行日志（日志记录失败时忽略，确保放行不被阻断）
        /// </summary>
        private async Task TryLogFailOpenAsync(string taskName, string message)
        {
            try
            {
                await AddLogAsync($"任务 [{taskName}] 锁基础设施异常，已放行执行（本次未参与并发控制）: {message}");
            }
            catch
            {
                // 日志记录失败不影响放行
            }
        }

        /// <summary>
        /// 记录跳过执行的日志
        /// </summary>
        protected virtual async Task LogSkipExecutionAsync(IJobExecutionContext context, string taskName)
        {
            await AddLogAsync($"任务 [{taskName}] 已达到最大并发数，本次执行被跳过");
        }
    }
}

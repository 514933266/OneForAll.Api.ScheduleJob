using OneForAll.Core;
using ScheduleJob.Application.Dtos;
using ScheduleJob.Application.Interfaces;
using ScheduleJob.Domain.Interfaces;
using ScheduleJob.Domain.Models;
using System.Threading.Tasks;

namespace ScheduleJob.Application
{
    /// <summary>
    /// 定时任务运行锁（OpenApi）
    /// </summary>
    public class JobLockService : IJobLockService
    {
        private readonly IJobLockManager _lockManager;

        public JobLockService(IJobLockManager lockManager)
        {
            _lockManager = lockManager;
        }

        /// <summary>
        /// 尝试获取执行资格（供任务节点远程调用，锁记录不存在时自动创建默认配置）
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="clientCode">客户端编号</param>
        /// <returns>检查结果</returns>
        public Task<JobLockCheckResult> TryAcquireAsync(string appId, string taskName, string clientCode)
        {
            return _lockManager.AcquireAsync(appId, clientCode, taskName, 1);
        }

        /// <summary>
        /// 尝试获取执行资格（可指定最大并发数，仅锁记录首次创建时生效）
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="clientCode">客户端编号</param>
        /// <param name="maxConcurrent">最大并发数</param>
        /// <returns>检查结果</returns>
        public Task<JobLockCheckResult> TryAcquireAsync(string appId, string taskName, string clientCode, int maxConcurrent)
        {
            return _lockManager.AcquireAsync(appId, clientCode, taskName, maxConcurrent);
        }

        /// <summary>
        /// 释放执行资格
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="version">锁版本号</param>
        /// <returns>结果</returns>
        public async Task<BaseErrType> ReleaseAsync(string appId, string taskName, int version)
        {
            var isSuccess = await _lockManager.ReleaseAsync(appId, taskName, version);
            return isSuccess ? BaseErrType.Success : BaseErrType.Fail;
        }

        /// <summary>
        /// 清理超过执行周期的残留锁（调度任务调用）
        /// </summary>
        /// <param name="config">检查配置</param>
        /// <returns>检查结果（含执行日志）</returns>
        public async Task<JobLockCleanupDto> CleanupExpiredAsync(JobLockCleanupConfig config)
        {
            var data = await _lockManager.CleanupExpiredAsync(config);
            var dto = new JobLockCleanupDto
            {
                TaskThresholdCount = data.TaskThresholdCount,
                DeletedHolderCount = data.DeletedHolderCount,
                AdjustedLockCount = data.AdjustedLockCount
            };

            dto.Logs.Add($"锁状态检查完成：共校验 {data.TaskThresholdCount} 个任务阈值（最低{data.MinMinutes}分钟/周期+{data.BufferMinutes}分钟/最高{data.MaxMinutes}分钟），释放 {data.DeletedHolderCount} 条残留锁，修正 {data.AdjustedLockCount} 条运行计数");
            return dto;
        }
    }
}

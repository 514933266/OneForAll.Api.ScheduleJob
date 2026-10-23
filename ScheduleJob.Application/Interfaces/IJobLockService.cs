using OneForAll.Core;
using ScheduleJob.Domain.Models;
using System.Threading.Tasks;

namespace ScheduleJob.Application.Interfaces
{
    /// <summary>
    /// 定时任务运行锁（OpenApi）
    /// </summary>
    public interface IJobLockService
    {
        /// <summary>
        /// 尝试获取执行资格（供任务节点远程调用，锁记录不存在时自动创建默认配置）
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="clientCode">客户端编号</param>
        /// <returns>检查结果</returns>
        Task<JobLockCheckResult> TryAcquireAsync(string appId, string taskName, string clientCode);

        /// <summary>
        /// 尝试获取执行资格（可指定最大并发数，仅锁记录首次创建时生效）
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="clientCode">客户端编号</param>
        /// <param name="maxConcurrent">最大并发数</param>
        /// <returns>检查结果</returns>
        Task<JobLockCheckResult> TryAcquireAsync(string appId, string taskName, string clientCode, int maxConcurrent);

        /// <summary>
        /// 释放执行资格
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="version">锁版本号</param>
        /// <returns>结果</returns>
        Task<BaseErrType> ReleaseAsync(string appId, string taskName, int version);

        /// <summary>
        /// 清理超过执行周期的残留锁（调度任务调用）
        /// </summary>
        /// <param name="config">检查配置</param>
        /// <returns>检查结果（含执行日志）</returns>
        Task<Dtos.JobLockCleanupDto> CleanupExpiredAsync(JobLockCleanupConfig config);
    }
}

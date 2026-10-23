using ScheduleJob.Domain.Models;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Interfaces
{
    /// <summary>
    /// 定时任务运行锁（领域规则：并发控制、残留锁清理）
    /// </summary>
    public interface IJobLockManager
    {
        /// <summary>
        /// 尝试获取执行资格（锁记录不存在时按传入上限自动创建默认配置）
        /// </summary>
        /// <param name="clientId">客户端id</param>
        /// <param name="clientCode">客户端编号</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="maxConcurrent">最大并发数（仅锁记录首次创建时生效）</param>
        /// <returns>检查结果</returns>
        Task<JobLockCheckResult> AcquireAsync(string clientId, string clientCode, string taskName, int maxConcurrent);

        /// <summary>
        /// 释放执行资格（运行计数减一 + 删除持锁记录）
        /// </summary>
        /// <param name="clientId">客户端id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="version">锁版本号</param>
        /// <returns>是否成功</returns>
        Task<bool> ReleaseAsync(string clientId, string taskName, int version);

        /// <summary>
        /// 清理超过执行周期的残留锁，并按实际持有数修正运行计数
        /// </summary>
        /// <param name="config">检查配置</param>
        /// <returns>检查结果</returns>
        Task<JobLockCleanupResult> CleanupExpiredAsync(JobLockCleanupConfig config);
    }
}

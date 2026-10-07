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
        /// 释放执行资格
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="version">锁版本号</param>
        /// <returns>结果</returns>
        Task<BaseErrType> ReleaseAsync(string appId, string taskName, int version);
    }
}

using OneForAll.EFCore;
using ScheduleJob.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Repositorys
{
    /// <summary>
    /// 定时任务运行锁仓储
    /// </summary>
    public interface IJobRunningLockRepository : IEFCoreRepository<JobRunningLock>
    {
        /// <summary>
        /// 获取锁实体（不跟踪）
        /// </summary>
        /// <param name="clientId">客户端id</param>
        /// <param name="taskName">任务名称</param>
        /// <param name="asNoTracking">是否不跟踪</param>
        /// <returns></returns>
        Task<JobRunningLock> GetAsync(string clientId, string taskName, bool asNoTracking);

        /// <summary>
        /// 更新锁计数（使用原始SQL避免并发冲突）
        /// </summary>
        /// <param name="id">实体id</param>
        /// <param name="currentRunningCount">当前运行数量</param>
        /// <param name="remark">备注</param>
        /// <param name="version">当前版本</param>
        /// <param name="originalVersion">历史版本</param>
        /// <returns></returns>
        Task<bool> UpdateLockCountAsync(Guid id, int currentRunningCount, string remark, int version, int originalVersion);
    }
}

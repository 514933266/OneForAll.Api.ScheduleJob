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
        /// 获取带排他锁的锁实体（用于高并发场景）
        /// 使用 SQL Server 的 UPDLOCK 和 ROWLOCK 提示，防止其他事务同时读取相同数据
        /// </summary>
        /// <param name="clientId">客户端id</param>
        /// <param name="taskName">任务名称</param>
        /// <param name="asNoTracking">是否不跟踪</param>
        /// <returns></returns>
        Task<JobRunningLock> GetWithLockAsync(string clientId, string taskName, bool asNoTracking);

        /// <summary>
        /// 增加运行计数（使用原始SQL避免并发冲突，数据库自动递增Version）
        /// </summary>
        /// <param name="id">实体id</param>
        /// <param name="originalVersion">历史版本（用于乐观锁条件判断）</param>
        /// <returns></returns>
        Task<bool> IncrementRunningCountAsync(Guid id, int originalVersion);

        /// <summary>
        /// 减少运行计数
        /// </summary>
        /// <param name="id">实体id</param>
        /// <returns></returns>
        Task<bool> DecrementRunningCountAsync(Guid id);

        /// <summary>
        /// /// 尝试获取锁并记录持有者（事务内 INSERT Holder + UPDATE 计数）
        /// 利用 job_lock_holder 表的唯一索引 (ClientId,TaskName,Version) 作为锁机制，
        /// 极限并发下唯一约束比应用层乐观锁更可靠
        /// </summary>
        /// <param name="lockId">运行锁id</param>
        /// <param name="clientId">客户端id</param>
        /// <param name="clientCode">客户端编码</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="originalVersion">历史版本</param>
        /// <param name="newVersion">当前版本</param>
        /// <returns></returns>
        Task<bool> TryAcquireWithHolderAsync(Guid lockId, string clientId, string clientCode, string taskName, int originalVersion, int newVersion);
    }
}

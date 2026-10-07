using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OneForAll.Core.ORM.Models;
using OneForAll.EFCore;
using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Repositorys;
using System;
using System.Threading.Tasks;

namespace ScheduleJob.Repository
{
    /// <summary>
    /// 定时任务运行锁仓储实现
    /// </summary>
    public class JobRunningLockRepository : Repository<JobRunningLock>, IJobRunningLockRepository
    {
        public JobRunningLockRepository(DbContext context)
            : base(context)
        {
        }

        /// <summary>
        /// 获取锁实体（不跟踪）
        /// </summary>
        /// <param name="clientId">客户端id</param>
        /// <param name="taskName">任务名称</param>
        /// <param name="asNoTracking">是否不跟踪</param>
        /// <returns></returns>
        public async Task<JobRunningLock> GetAsync(string clientId, string taskName, bool asNoTracking)
        {
            if (asNoTracking)
            {
                return await DbSet.AsNoTracking().FirstOrDefaultAsync(w => w.ClientId == clientId && w.TaskName == taskName);
            }
            else
            {
                return await DbSet.FirstOrDefaultAsync(w => w.ClientId == clientId && w.TaskName == taskName);
            }
        }

        /// <summary>
        /// 获取带排他锁的锁实体（用于高并发场景）
        /// 使用 SQL Server 的 UPDLOCK 和 ROWLOCK 提示，防止其他事务同时读取相同数据
        /// </summary>
        /// <param name="clientId">客户端id</param>
        /// <param name="taskName">任务名称</param>
        /// <param name="asNoTracking">是否不跟踪</param>
        /// <returns></returns>
        public async Task<JobRunningLock> GetWithLockAsync(string clientId, string taskName, bool asNoTracking)
        {
            var sql = "SELECT * FROM job_running_lock WITH (UPDLOCK, ROWLOCK) WHERE ClientId = {0} AND TaskName = {1}";
            if (asNoTracking)
            {
                return await DbSet.FromSqlRaw(sql, clientId, taskName).AsNoTracking().FirstOrDefaultAsync();
            }
            else
            {
                return await DbSet.FromSqlRaw(sql, clientId, taskName).FirstOrDefaultAsync();
            }
        }

        /// <summary>
        /// 增加运行计数（使用原始SQL避免并发冲突，数据库自动递增Version）
        /// </summary>
        /// <param name="id">实体id</param>
        /// <param name="originalVersion">历史版本（用于乐观锁条件判断）</param>
        /// <returns></returns>
        public async Task<bool> IncrementRunningCountAsync(Guid id, int originalVersion)
        {
            var affectedRows = await Context.Database.ExecuteSqlRawAsync(
                "UPDATE job_running_lock SET CurrentRunningCount = CurrentRunningCount+1, Version = Version + 1, UpdateTime = {0} WHERE Id = {1} AND Version = {2}",
                DateTime.UtcNow,
                id,
                originalVersion);

            return affectedRows > 0;
        }

        /// <summary>
        /// 减少运行计数
        /// </summary>
        /// <param name="id">实体id</param>
        /// <returns></returns>
        public async Task<bool> DecrementRunningCountAsync(Guid id)
        {
            var affectedRows = await Context.Database.ExecuteSqlRawAsync(
                "UPDATE job_running_lock SET CurrentRunningCount = CurrentRunningCount - 1, UpdateTime = {0} WHERE Id = {1}",
                DateTime.UtcNow,
                id);

            return affectedRows > 0;
        }

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
        public async Task<bool> TryAcquireWithHolderAsync(Guid lockId, string clientId, string clientCode, string taskName, int originalVersion, int newVersion)
        {
            using var transaction = await Context.Database.BeginTransactionAsync();
            try
            {
                // 1. INSERT Holder 记录 —— 利用唯一索引作为真正的锁
                // 并发场景下，相同 (ClientId, TaskName, Version) 只有一个能插入成功
                await Context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO job_lock_holder (Id, ClientId, ClientCode, TaskName, Version, LockTime) VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
                    Guid.NewGuid(), clientId, clientCode, taskName, newVersion, DateTime.UtcNow);

                // 2. UPDATE 运行计数 —— 乐观锁作为二次校验
                var affected = await Context.Database.ExecuteSqlRawAsync(
                    "UPDATE job_running_lock SET CurrentRunningCount = CurrentRunningCount + 1, Version = {0}, UpdateTime = {1} WHERE Id = {2} AND Version = {3}",
                    newVersion, DateTime.UtcNow, lockId, originalVersion);

                if (affected == 0)
                {
                    // 乐观锁冲突：Holder 已插入但 RunningLock 校验失败，回滚全部
                    await SafeRollbackAsync(transaction);
                    return false;
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex) when (IsUniqueIndexConflict(ex))
            {
                // INSERT Holder 唯一约束冲突（2601/2627）→ 并发竞争失败，返回 false
                await SafeRollbackAsync(transaction);
                return false;
            }
            catch
            {
                // 其他异常（锁表未创建 208、连接失败、超时、死锁等）→ 回滚后向上抛出，
                // 由调用方（BaseLockJob）按配置决定放行或跳过
                await SafeRollbackAsync(transaction);
                throw;
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
        /// 安全回滚事务（回滚失败时忽略，避免掩盖原始异常）
        /// </summary>
        private static async Task SafeRollbackAsync(IDbContextTransaction transaction)
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch
            {
            }
        }

    }
}

using Microsoft.EntityFrameworkCore;
using OneForAll.Core.ORM.Models;
using OneForAll.EFCore;
using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Repositorys;
using System;
using System.Linq;
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
        /// 更新锁计数（使用原始SQL避免并发冲突）
        /// </summary>
        /// <param name="id">实体id</param>
        /// <param name="currentRunningCount">当前运行数量</param>
        /// <param name="remark">备注</param>
        /// <param name="version">当前版本</param>
        /// <param name="originalVersion">历史版本</param>
        /// <returns></returns>
        public async Task<bool> UpdateLockCountAsync(Guid id, int currentRunningCount, string remark, int version, int originalVersion)
        {
            var affectedRows = await Context.Database.ExecuteSqlRawAsync(
                "UPDATE job_running_lock SET CurrentRunningCount = {0}, Version = {1}, UpdateTime = {2}, Remark = {3} WHERE Id = {4} AND Version = {5}",
                currentRunningCount,
                version,
                DateTime.UtcNow,
                remark,
                id,
                originalVersion);

            return affectedRows > 0;
        }
    }
}

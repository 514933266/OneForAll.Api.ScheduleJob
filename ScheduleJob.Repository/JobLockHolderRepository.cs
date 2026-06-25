using Microsoft.EntityFrameworkCore;
using OneForAll.EFCore;
using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Repositorys;

namespace ScheduleJob.Repository
{
    /// <summary>
    /// 定时任务锁持有者仓储实现
    /// </summary>
    public class JobLockHolderRepository : Repository<JobLockHolder>, IJobLockHolderRepository
    {
        public JobLockHolderRepository(DbContext context)
            : base(context)
        {
        }
    }
}

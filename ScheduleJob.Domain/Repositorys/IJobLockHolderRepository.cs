using OneForAll.EFCore;
using ScheduleJob.Domain.Entities;

namespace ScheduleJob.Domain.Repositorys
{
    /// <summary>
    /// 定时任务锁持有者仓储
    /// </summary>
    public interface IJobLockHolderRepository : IEFCoreRepository<JobLockHolder>
    {
    }
}

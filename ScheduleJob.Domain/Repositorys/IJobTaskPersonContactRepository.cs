using OneForAll.EFCore;
using ScheduleJob.Domain.AggregateRoots;
using ScheduleJob.Domain.Aggregates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Repositorys
{
    /// <summary>
    /// 负责人
    /// </summary>
    public interface IJobTaskPersonContactRepository : IEFCoreRepository<JobTaskPersonContact>
    {
        /// <summary>
        /// 查询负责人列表
        /// </summary>
        /// <param name="taskIds">定时任务id</param>
        /// <returns>列表</returns>
        Task<IEnumerable<JobTaskPersonAggr>> GetListPersonAsync(IEnumerable<Guid> taskIds);
    }
}

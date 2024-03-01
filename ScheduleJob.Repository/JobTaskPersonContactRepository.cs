using Microsoft.EntityFrameworkCore;
using OneForAll.EFCore;
using ScheduleJob.Domain.AggregateRoots;
using ScheduleJob.Domain.Aggregates;
using ScheduleJob.Domain.Repositorys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Repository
{
    /// <summary>
    /// 负责人
    /// </summary>
    public class JobTaskPersonContactRepository : Repository<JobTaskPersonContact>, IJobTaskPersonContactRepository
    {
        public JobTaskPersonContactRepository(DbContext context)
            : base(context)
        {
        }

        /// <summary>
        /// 查询负责人列表
        /// </summary>
        /// <param name="taskIds">定时任务id</param>
        /// <returns>列表</returns>
        public async Task<IEnumerable<JobTaskPersonAggr>> GetListPersonAsync(IEnumerable<Guid> taskIds)
        {
            var dbSet = DbSet.Where(w => taskIds.Contains(w.JobTaskId));
            var personDbSet = Context.Set<JobPerson>();

            var sql = (from contact in dbSet
                       join person in personDbSet on contact.JobPersonId equals person.Id
                       select new JobTaskPersonAggr()
                       {
                           Id = person.Id,
                           Name = person.Name,
                           Email = person.Email,
                           Mobile = person.Mobile,
                           SysUserId = person.SysUserId,
                           JobTaskId = contact.JobTaskId
                       });

            return await sql.ToListAsync();
        }
    }
}

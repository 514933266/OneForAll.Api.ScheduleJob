using Microsoft.EntityFrameworkCore;
using OneForAll.EFCore;
using ScheduleJob.Domain.AggregateRoots;
using ScheduleJob.Domain.Repositorys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Repository
{
    /// <summary>
    /// 人员信息
    /// </summary>
    public class JobPersonRepository : Repository<JobPerson>, IJobPersonRepository
    {
        public JobPersonRepository(DbContext context)
            : base(context)
        {
        }
    }
}

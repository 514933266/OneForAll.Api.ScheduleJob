using OneForAll.EFCore;
using ScheduleJob.Domain.AggregateRoots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Repositorys
{
    /// <summary>
    /// 人员信息
    /// </summary>
    public interface IJobPersonRepository : IEFCoreRepository<JobPerson>
    {
    }
}

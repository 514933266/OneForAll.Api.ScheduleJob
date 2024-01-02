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
    /// 系统通知设置
    /// </summary>
    public class JobNotificationConfigRepository : Repository<JobNotificationConfig>, IJobNotificationConfigRepository
    {
        public JobNotificationConfigRepository(DbContext context)
            : base(context)
        {
        }
    }
}

using ScheduleJob.Domain.AggregateRoots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Aggregates
{
    /// <summary>
    /// 定时任务负责人
    /// </summary>
    public class JobTaskPersonAggr : JobPerson
    {
        /// <summary>
        /// 定时任务id
        /// </summary>
        public Guid JobTaskId { get; set; }
    }
}

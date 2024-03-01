using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.AggregateRoots
{
    /// <summary>
    /// 负责人
    /// </summary>
    public class JobTaskPersonContact
    {
        /// <summary>
        /// 数据id
        /// </summary>
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// 定时任务id
        /// </summary>
        [Required]
        public Guid JobTaskId { get; set; }

        /// <summary>
        /// 用户id
        /// </summary>
        [Required]
        public Guid JobPersonId { get; set; }

    }
}

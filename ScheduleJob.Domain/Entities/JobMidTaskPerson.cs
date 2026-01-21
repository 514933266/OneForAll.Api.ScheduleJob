using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Entities
{
    /// <summary>
    /// 负责人
    /// </summary>
    public class JobMidTaskPerson
    {
        /// <summary>
        /// 数据id
        /// </summary>
        [Key]
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// 定时任务id
        /// </summary>
        [Required]
        public Guid JobTaskId { get; set; }

        /// <summary>
        /// OA系统用户id
        /// </summary>
        [Required]
        public Guid OAPersonId { get; set; }

    }
}

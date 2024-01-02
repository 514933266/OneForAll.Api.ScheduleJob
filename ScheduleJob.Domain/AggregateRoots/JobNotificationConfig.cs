using ScheduleJob.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.AggregateRoots
{
    /// <summary>
    /// 通知设置
    /// </summary>
    public class JobNotificationConfig
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// 通知类型
        /// </summary>
        [Required]
        public JobNotificationTypeEnum NotificationType { get; set; }

        /// <summary>
        /// 目标值：[]
        /// </summary>
        [Required]
        public string TargetJson { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Column(TypeName = "datetime")]
        public DateTime CreateTime { get; set; }
    }
}

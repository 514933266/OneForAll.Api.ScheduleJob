using ScheduleJob.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Models
{
    /// <summary>
    /// 定时任务日志
    /// </summary>
    public class JobTaskLogForm
    {
        /// <summary>
        /// 主键
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 应用程序id
        /// </summary>
        [Required]
        [StringLength(50)]
        public string AppId { get; set; }

        /// <summary>
        ///任务名称
        /// </summary>
        [Required]
        [StringLength(100)]
        public string TaskName { get; set; }

        /// <summary>
        /// 日志内容
        /// </summary>
        [Required]
        public string Content { get; set; }

        /// <summary>
        /// 类型
        /// </summary>
        [Required]
        public JobTaskLogTypeEnum Type { get; set; }
    }
}
